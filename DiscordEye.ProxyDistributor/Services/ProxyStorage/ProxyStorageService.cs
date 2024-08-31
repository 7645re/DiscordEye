using System.Collections.Concurrent;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Options;
using Microsoft.Extensions.Options;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public class ProxyStorageService : IProxyStorageService
{
    private readonly IList<Proxy> _proxies;
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
            .ToList();
        _proxiesQueue = new ConcurrentQueue<Proxy>(_proxies);
        _logger.LogInformation($"Proxies were loaded in the amount of {_proxies.Count} pieces");
    }

    public Proxy? TakeProxy(string serviceName)
    {
        lock (_proxiesQueue)
        {
            var usingProxy = _proxies
                .SingleOrDefault(x => x.WhoUsing == serviceName);
            if (usingProxy is not null)
            {
                var whoUsing = usingProxy.WhoUsing;
                if (!usingProxy.Release())
                    throw new ArgumentException($"Proxy {usingProxy.Id} could not be released");

                _logger.LogInformation($"Proxy {usingProxy.Id} was released by '{whoUsing}'");
            }
            
            var freeProxy = _proxies
                .FirstOrDefault(x => 
                    x.IsFree() 
                    && x.Id != usingProxy?.Id);
            if (freeProxy is null)
                return null;
            if (!freeProxy.Take(serviceName))
                throw new ArgumentException($"Proxy {freeProxy.Id} cannot be taken");
            
            _logger.LogInformation($"Proxy {freeProxy.Id} was taken by the '{freeProxy.WhoUsing}'");
            return freeProxy;
        }
    }
}