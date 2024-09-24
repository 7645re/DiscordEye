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
    private readonly ConcurrentDictionary<Guid, ProxyHeartbeat> _proxiesHeartbeatsDict;
    private readonly TimeSpan _healthPulsePeriod = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, GrpcChannel> _cachedGrpcChannel;
    private readonly IProxyReservationService _proxyReservationService;
    private readonly ILogger<ProxyHeartbeatService> _logger;
    private readonly IProxyHeartbeatSnapShooter _snapShooter;
    private readonly LinkedList<ProxyHeartbeat> _proxiesHeartbeatsLList;
    private readonly KeyedLockService _lockService;
    
    public ProxyHeartbeatService(
        IProxyReservationService proxyReservationService,
        ILogger<ProxyHeartbeatService> logger,
        IProxyHeartbeatSnapShooter snapShooter,
        KeyedLockService lockService)
    {
        _proxyReservationService = proxyReservationService;
        _logger = logger;
        _snapShooter = snapShooter;
        _lockService = lockService;
        var snapShoot = _snapShooter
            .LoadSnapShotAsync()
            .GetAwaiter()
            .GetResult() ?? ImmutableDictionary<Guid, ProxyHeartbeat>.Empty;
        _proxiesHeartbeatsDict = new ConcurrentDictionary<Guid, ProxyHeartbeat>(snapShoot);
        _proxiesHeartbeatsLList = new LinkedList<ProxyHeartbeat>(snapShoot
            .Select(x => x.Value));
        _cachedGrpcChannel = new ConcurrentDictionary<string, GrpcChannel>();
        _logger.LogInformation($"Loaded {_proxiesHeartbeatsDict.Count} proxies heartbeats and " +
                               $"{_proxiesHeartbeatsLList.Count} proxies heartbeats in linked list");
    }

    public async Task<bool> RegisterProxyHeartbeat(ProxyHeartbeat proxyHeartbeat)
    {
        using (await _lockService.LockAsync("ProxyHeartbeat"))
        {
            if (_proxiesHeartbeatsDict.TryAdd(proxyHeartbeat.ProxyId, proxyHeartbeat) == false)
            {
                _logger.LogWarning($"Error registering proxy {proxyHeartbeat.ProxyId} " +
                                   $"to heartbeat service {proxyHeartbeat}");
                return false;
            }

            _proxiesHeartbeatsLList.AddLast(proxyHeartbeat);
            _logger.LogInformation($"Proxy {proxyHeartbeat.ProxyId} registered to heartbeat service");
            await _snapShooter.SnapShootAsync(_proxiesHeartbeatsDict);
            return true;
        }
    }

    public async Task<bool> UnRegisterProxyHeartbeat(Guid proxyId)
    {
        using (await _lockService.LockAsync("ProxyHeartbeat"))
        {
            if (_proxiesHeartbeatsDict.TryGetValue(proxyId, out var proxyHeartbeat) == false)
            {
                _logger.LogWarning($"Proxy {proxyId} not found in heartbeat service");
                return false;
            }
        
            if (_proxiesHeartbeatsDict.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                    proxyId,
                    proxyHeartbeat)) == false)
            {
                _logger.LogWarning($"Error unregistering proxy {proxyId} " +
                                   $"from heartbeat service {proxyHeartbeat}");
                return false;
            }

            _proxiesHeartbeatsLList.Remove(proxyHeartbeat);
            _logger.LogInformation($"Proxy {proxyId} unregistered from heartbeat service");
            await _snapShooter.SnapShootAsync(_proxiesHeartbeatsDict);
            return true;
        }
    }
    
    //TODO: mb can parallel grpc calls
    public async Task PulseProxiesHeartbeats()
    {
        using (await _lockService.LockAsync("ProxyHeartbeat"))
        {
            _logger.LogInformation($"Pulsing {_proxiesHeartbeatsLList.Count} proxies heartbeats");
            var proxyHeartbeatNode = _proxiesHeartbeatsLList.First;
            while (proxyHeartbeatNode != null)
            {
                var proxyHeartbeat = proxyHeartbeatNode.Value;
                if (DateTime.Now - proxyHeartbeat.LastHeartbeatDatetime < _healthPulsePeriod)
                {
                    continue;
                }

                if (await PulseProxyHeartbeat(proxyHeartbeat) == false)
                {
                    _proxiesHeartbeatsDict.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                        proxyHeartbeat.ProxyId,
                        proxyHeartbeat));
                    _proxiesHeartbeatsLList.Remove(proxyHeartbeat);
                    proxyHeartbeatNode = proxyHeartbeatNode.Next;
                    await _proxyReservationService.ReleaseProxy(proxyHeartbeat.ProxyId, proxyHeartbeat.ReleaseKey);
                    await _snapShooter.SnapShootAsync(_proxiesHeartbeatsDict);
                    continue;
                }

                var newReservationDateTime = DateTime.Now + _healthPulsePeriod;
                if (await _proxyReservationService.ProlongProxy(proxyHeartbeat.ProxyId, newReservationDateTime) == false)
                {
                    _proxiesHeartbeatsDict.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                        proxyHeartbeat.ProxyId,
                        proxyHeartbeat));
                    _proxiesHeartbeatsLList.Remove(proxyHeartbeat);
                    proxyHeartbeatNode = proxyHeartbeatNode.Next;
                    await _proxyReservationService.ReleaseProxy(proxyHeartbeat.ProxyId, proxyHeartbeat.ReleaseKey);
                    await _snapShooter.SnapShootAsync(_proxiesHeartbeatsDict);
                    continue;
                }

                var newHeartbeat = proxyHeartbeat with { LastHeartbeatDatetime = newReservationDateTime };
                _proxiesHeartbeatsLList.Remove(proxyHeartbeat);
                _proxiesHeartbeatsLList.AddLast(newHeartbeat);
                proxyHeartbeatNode = proxyHeartbeatNode.Next;
                _logger.LogInformation($"Pulsed proxy {proxyHeartbeat.ProxyId}");
            }
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