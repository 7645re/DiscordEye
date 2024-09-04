using System.Collections.Concurrent;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Options;
using Microsoft.Extensions.Options;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public class ProxyStorageService : IProxyStorageService
{
    private readonly Proxy[] _proxies;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly ILogger<ProxyStorageService> _logger;
    
    public ProxyStorageService(
        IOptions<ProxiesOptions> proxiesOptions,
        ILogger<ProxyStorageService> logger)
    {
        _logger = logger;
        _proxies = proxiesOptions
            .Value
            .Proxies
            .Select(x => x.ToProxy())
            .ToArray();
        _proxiesQueue = new ConcurrentQueue<Proxy>(_proxies);
        _logger.LogInformation($"Proxies were loaded in the amount of {_proxies.Length} pieces");
    }

    public ProxyInfo[] GetProxies()
    {
        return _proxies.Select(x => x.ToProxyInfo()).ToArray();
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

    public bool TryTakeProxy(out (Proxy takenProxy, Guid releaseKey)? takenProxyWithKey)
    {
        while (!_proxiesQueue.IsEmpty)
        {
            if (!_proxiesQueue.TryDequeue(out var proxy))
                continue;

            if (!proxy.IsFree())
                throw new InvalidOperationException($"Proxy {proxy.Id} is not free, but should be");

            if (!proxy.TryTake(out var releaseKey))
                continue;
    
            _logger.LogInformation($"Proxy {proxy.Id} was taken");
            takenProxyWithKey = (proxy, releaseKey.Value);
            return true;
        }
        
        _logger.LogWarning("All proxies were taken");
        takenProxyWithKey = null;
        return false;
    }
}