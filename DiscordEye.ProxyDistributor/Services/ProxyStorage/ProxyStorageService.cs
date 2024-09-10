using System.Collections.Concurrent;
using DiscordEye.ProxyDistributor.BackgroundServices;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public class ProxyStorageService : IProxyStorageService
{
    private readonly Proxy[] _proxies;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly ILogger<ProxyStorageService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProxyStorageService(
        Proxy[] proxies,
        ILogger<ProxyStorageService> logger,
        IServiceProvider serviceProvider)
    {
        _proxies = proxies;
        _proxiesQueue = new ConcurrentQueue<Proxy>(_proxies);
        _logger = logger;
        _serviceProvider = serviceProvider;
        _logger.LogInformation($"Proxies were loaded in the amount of {_proxies.Length} pieces");
    }

    public ProxyDto[] GetProxies()
    {
        return _proxies.Select(x => x.ToProxyDto()).ToArray();
    }
    
    public bool TryReleaseProxy(int proxyId, Guid releaseKey)
    {
        var proxy = _proxies
            .SingleOrDefault(x => x.Id == proxyId);
        if (proxy is null || proxy.TryRelease(releaseKey))
        {
            _logger.LogWarning($"Proxy {proxyId} was not found or was not released");
            return false;
        }
        
        _proxiesQueue.Enqueue(proxy);
        _logger.LogInformation($"Proxy {proxy.Id} was released");
        return true;
    }

    public bool TryTakeProxy(string nodeAddress, out (Proxy takenProxy, Guid releaseKey)? takenProxyWithKey)
    {
        while (!_proxiesQueue.IsEmpty)
        {
            if (!_proxiesQueue.TryDequeue(out var proxy))
                continue;

            if (!proxy.IsFree())
                throw new InvalidOperationException($"Proxy {proxy.Id} is not free, but should be");

            if (!proxy.TryTake(nodeAddress, out var releaseKey))
                continue;
    
            _logger.LogInformation($"Proxy {proxy.Id} was taken");
            var heartbeatService = _serviceProvider.GetHostedService<ProxyHeartbeatBackgroundService>();
            if (heartbeatService is null)
            {
                _logger.LogWarning("Heartbeat service was not found");
                takenProxyWithKey = null;
                return false;
            }
            
            if (!heartbeatService.TryRegisterProxy(proxy))
            {
                _logger.LogWarning($"Proxy {proxy.Id} was not registered to heartbeat service");
                if (releaseKey is not null && !proxy.TryRelease(releaseKey.Value))
                {
                    throw new InvalidOperationException($"Proxy {proxy.Id} was not released " +
                                                        $"after heartbeat failure");
                }
                _proxiesQueue.Enqueue(proxy);
                takenProxyWithKey = null;
                return false;
            }
            takenProxyWithKey = (proxy, releaseKey.Value);
            return true;
        }
        
        _logger.LogWarning("All proxies were taken");
        takenProxyWithKey = null;
        return false;
    }

    public bool TryForceReleaseProxy(int id)
    {
        var proxy = _proxies.SingleOrDefault(x => x.Id == id);
        if (proxy is null)
            throw new ArgumentNullException($"Proxy with id {id} doesnt exist");

        if (proxy.TryForceRelease()) return true;
        _logger.LogInformation($"Cannot force release proxy with id {proxy.Id}");
        return false;

    }
}