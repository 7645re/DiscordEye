using System.Collections.Concurrent;
using System.Collections.Immutable;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using DiscordEye.ProxyDistributor.Services.SnapShoot;
using Grpc.Core;
using Grpc.Net.Client;

namespace DiscordEye.ProxyDistributor.Services.Heartbeat;

public class ProxyHeartbeatService : IProxyHeartbeatService
{
    private readonly ConcurrentQueue<ProxyHeartbeat> _proxiesHeartbeatsQueue;
    private readonly ConcurrentDictionary<Guid, ProxyHeartbeat> _proxiesHeartbeats;
    private readonly TimeSpan _healthPulsePeriod = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, GrpcChannel> _cachedGrpcChannel;
    private readonly IProxyReservationService _proxyReservationService;
    private readonly ILogger<ProxyHeartbeatService> _logger;
    private readonly IProxyHeartbeatSnapShooter _snapShooter;

    public ProxyHeartbeatService(
        IProxyReservationService proxyReservationService,
        ILogger<ProxyHeartbeatService> logger,
        IProxyHeartbeatSnapShooter snapShooter)
    {
        _proxyReservationService = proxyReservationService;
        _logger = logger;
        _snapShooter = snapShooter;
        var snapShoot = _snapShooter
            .LoadSnapShotAsync()
            .GetAwaiter()
            .GetResult() ?? ImmutableDictionary<Guid, ProxyHeartbeat>.Empty;
        _proxiesHeartbeatsQueue = new ConcurrentQueue<ProxyHeartbeat>(
            snapShoot.Select(x => x.Value));
        _proxiesHeartbeats = new ConcurrentDictionary<Guid, ProxyHeartbeat>(snapShoot);
        _cachedGrpcChannel = new ConcurrentDictionary<string, GrpcChannel>();
        _logger.LogInformation($"Loaded {_proxiesHeartbeats.Count} proxies heartbeats and " +
                               $"{_proxiesHeartbeatsQueue.Count} proxies heartbeats in queue");
    }

    public async Task<bool> RegisterProxyHeartbeat(ProxyHeartbeat proxyHeartbeat)
    {
        if (_proxiesHeartbeats.TryAdd(proxyHeartbeat.ProxyId, proxyHeartbeat) == false)
        {
            _logger.LogWarning($"Error registering proxy {proxyHeartbeat.ProxyId} " +
                               $"to heartbeat service {proxyHeartbeat}");
            return false;
        }

        _proxiesHeartbeatsQueue.Enqueue(proxyHeartbeat);
        _logger.LogInformation($"Proxy {proxyHeartbeat.ProxyId} registered to heartbeat service");
        await _snapShooter.SnapShootAsync(_proxiesHeartbeats);
        return true;
    }

    public async Task<bool> UnRegisterProxyHeartbeat(Guid proxyId)
    {
        if (_proxiesHeartbeats.TryGetValue(proxyId, out var proxyHeartbeat) == false)
        {
            _logger.LogWarning($"Proxy {proxyId} not found in heartbeat service");
            return false;
        }
        
        if (_proxiesHeartbeats.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                proxyId,
                proxyHeartbeat)) == false)
        {
            _logger.LogWarning($"Error unregistering proxy {proxyId} " +
                               $"from heartbeat service {proxyHeartbeat}");
            return false;
        }

        proxyHeartbeat.IsDead = true;
        _logger.LogInformation($"Proxy {proxyId} marked as dead, unregistered from heartbeat service");
        await _snapShooter.SnapShootAsync(_proxiesHeartbeats);
        return true;
    }
    
    //TODO: mb can parallel grpc calls
    public async Task PulseProxiesHeartbeats()
    {
        _logger.LogInformation($"Pulsing {_proxiesHeartbeatsQueue.Count} proxies heartbeats");
        while (_proxiesHeartbeatsQueue.IsEmpty == false)
        {
            if (_proxiesHeartbeatsQueue.TryDequeue(out var proxyHeartbeat) == false)
            {
                continue;
            }

            if (proxyHeartbeat.IsDead)
            {
                _logger.LogWarning($"Proxy {proxyHeartbeat.ProxyId} is dead");
                continue;
            }
            
            if (DateTime.Now - proxyHeartbeat.LastHeartbeatDatetime < _healthPulsePeriod)
            {
                _proxiesHeartbeatsQueue.Enqueue(proxyHeartbeat);
                continue;
            }

            if (await PulseProxyHeartbeat(proxyHeartbeat) == false)
            {
                _proxiesHeartbeats.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                    proxyHeartbeat.ProxyId,
                    proxyHeartbeat));
                await _proxyReservationService.ReleaseProxy(proxyHeartbeat.ProxyId, proxyHeartbeat.ReleaseKey);
                await _snapShooter.SnapShootAsync(_proxiesHeartbeats);
                continue;
            }

            var newReservationDateTime = DateTime.Now + _healthPulsePeriod;
            if (await _proxyReservationService.ProlongProxy(proxyHeartbeat.ProxyId, newReservationDateTime) == false)
            {
                _proxiesHeartbeats.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                    proxyHeartbeat.ProxyId,
                    proxyHeartbeat));
                await _proxyReservationService.ReleaseProxy(proxyHeartbeat.ProxyId, proxyHeartbeat.ReleaseKey);
                await _snapShooter.SnapShootAsync(_proxiesHeartbeats);
                continue;
            }

            var newHeartbeat = proxyHeartbeat with { LastHeartbeatDatetime = newReservationDateTime };
            _proxiesHeartbeatsQueue.Enqueue(newHeartbeat);
            _logger.LogInformation($"Pulsed proxy {proxyHeartbeat.ProxyId}");
        }
    }

    private async Task<bool> PulseProxyHeartbeat(ProxyHeartbeat proxyHeartbeat)
    {
        var grpcChannel = GetOrCreateGrpcChannel(proxyHeartbeat.NodeAddress);
        if (grpcChannel is null)
        {
            _logger.LogWarning($"Unable to make a heartbeat for {proxyHeartbeat}" +
                               $" because the grpc channel was not created");
            return false;
        }
        var client = new ProxyHeartbeatGrpcService.ProxyHeartbeatGrpcServiceClient(grpcChannel);

        try
        {
            var response = await client.HeartbeatAsync(new ProxyHeartbeatRequest());
            if (Guid.TryParse(response.ReleaseKey, out var parsedReleaseKey) == false
                || proxyHeartbeat.ReleaseKey != parsedReleaseKey)
            {
                _logger.LogWarning($"Invalid release key for proxy {proxyHeartbeat.ProxyId}");
                return false;
            }

            return true;
        }
        catch (RpcException e)
        {
            _logger.LogWarning($"Failed to pulse proxy {proxyHeartbeat.ProxyId}, {e}");
            return false;
        }
    }
    
     private GrpcChannel? GetOrCreateGrpcChannel(string address)
     {
         if (_cachedGrpcChannel.TryGetValue(address, out var channel))
         {
             _logger.LogInformation($"Found cached grpc channel for {address}");
             return channel;
         }

         var newChannel = CreateGrpcChannel($"http://{address}");
         if (newChannel is null)
         {
             return null;
         }
         
         if (!_cachedGrpcChannel.TryAdd(address, newChannel))
         {
             _logger.LogWarning($"Failed to cache grpc channel for {address}");
             return null;
         }
        
         _logger.LogInformation($"Created new grpc channel for {address}");
         return newChannel;
     }
     
     private GrpcChannel? CreateGrpcChannel(string uri)
     {
         try
         {
            var channel = GrpcChannel.ForAddress(uri);
            return channel;
         }
         catch (Exception e)
         {
             _logger.LogWarning("An error occurred while creating an grpc channel for address {uri}", e);
             return null;
         }
     }
}