using System.Collections.Concurrent;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using Grpc.Core;
using Grpc.Net.Client;

namespace DiscordEye.ProxyDistributor.Services.Heartbeat;

public class ProxyHeartbeatService : IProxyHeartbeatService
{
    private readonly ConcurrentQueue<ProxyHeartbeat> _proxiesHeartbeatsQueue;
    private readonly ConcurrentDictionary<Guid, ProxyHeartbeat> _proxiesHeartbeats;
    private readonly KeyedLockService _locker;
    private readonly TimeSpan _healthPulsePeriod = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, GrpcChannel> _cachedGrpcChannel;
    private readonly IProxyReservationService _proxyReservationService;
    private const string RegisteredProxiesIdsKey = "registeredProxiesIds";

    public ProxyHeartbeatService(
        KeyedLockService locker,
        IProxyReservationService proxyReservationService)
    {
        _locker = locker;
        _proxyReservationService = proxyReservationService;
        _proxiesHeartbeatsQueue = new();
        _proxiesHeartbeats = new();
        _cachedGrpcChannel = new();
    }

    public bool RegisterProxyHeartbeat(ProxyHeartbeat proxyHeartbeat)
    {
        using (_locker.Lock(RegisteredProxiesIdsKey))
        {
            if (_proxiesHeartbeats.TryAdd(proxyHeartbeat.ProxyId, proxyHeartbeat) == false)
            {
                return false;
            }

            _proxiesHeartbeatsQueue.Enqueue(proxyHeartbeat);
            return true;
        }
    }

    public bool UnRegisterProxyHeartbeat(Guid proxyId)
    {
        using (_locker.Lock(RegisteredProxiesIdsKey))
        {
            if (_proxiesHeartbeats.TryGetValue(proxyId, out var proxyHeartbeat) == false)
            {
                return false;
            }

            if (_proxiesHeartbeats.TryRemove(new KeyValuePair<Guid, ProxyHeartbeat>(
                    proxyId,
                    proxyHeartbeat)) == false)
            {
                return false;
            }

            proxyHeartbeat.IsDead = true;
            return true;
        }
    }
    
    //TODO: mb can parallel grpc calls
    public async Task PulseProxiesHeartbeats()
    {
        while (_proxiesHeartbeatsQueue.IsEmpty == false)
        {
            if (_proxiesHeartbeatsQueue.TryDequeue(out var proxyHeartbeat) == false)
            {
                continue;
            }

            if (proxyHeartbeat.IsDead)
            {
                continue;
            }
            
            if (DateTime.Now - proxyHeartbeat.LastHeartbeatDatetime < _healthPulsePeriod)
            {
                continue;
            }

            if (await PulseProxyHeartbeat(proxyHeartbeat) == false)
            {
                await _proxyReservationService.ReleaseProxy(proxyHeartbeat.ProxyId, proxyHeartbeat.ReleaseKey);
                continue;
            }

            var newReservationDateTime = DateTime.Now + _healthPulsePeriod;
            if (await _proxyReservationService.ProlongProxy(proxyHeartbeat.ProxyId, newReservationDateTime) == false)
            {
                await _proxyReservationService.ReleaseProxy(proxyHeartbeat.ProxyId, proxyHeartbeat.ReleaseKey);
                continue;
            }

            var newHeartbeat = proxyHeartbeat with { LastHeartbeatDatetime = newReservationDateTime };
            _proxiesHeartbeatsQueue.Enqueue(newHeartbeat);
        }
    }

    private async Task<bool> PulseProxyHeartbeat(ProxyHeartbeat proxyHeartbeat)
    {
        var grpcChannel = GetOrCreateGrpcChannel(proxyHeartbeat.NodeAddress);
        var client = new ProxyHeartbeatGrpcService.ProxyHeartbeatGrpcServiceClient(grpcChannel);

        try
        {
            var response = await client.HeartbeatAsync(new ProxyHeartbeatRequest());
            if (Guid.TryParse(response.ReleaseKey, out var parsedReleaseKey) == false
                || proxyHeartbeat.ReleaseKey != parsedReleaseKey)
            {
                return false;
            }

            return true;
        }
        catch (RpcException)
        {
            return false;
        }
    }
    
     private GrpcChannel? GetOrCreateGrpcChannel(string address)
     {
         if (_cachedGrpcChannel.TryGetValue(address, out var channel))
         {
             return channel;
         }

         var newChannel = GrpcChannel.ForAddress($"http://{address}");
         if (!_cachedGrpcChannel.TryAdd(address, newChannel))
         {
             return null;
         }

         return newChannel;
     }
}