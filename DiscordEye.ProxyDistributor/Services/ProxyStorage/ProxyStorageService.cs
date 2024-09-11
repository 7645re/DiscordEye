using System.Collections.Concurrent;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.BackgroundServices;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.Node.Data;
using DiscordEye.ProxyDistributor.Services.Node.Manager;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public class ProxyStorageService : IProxyStorageService
{
    private readonly Proxy[] _proxies;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly ILogger<ProxyStorageService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITakerNodesManager _takerNodesManager;
    private readonly KeyedLockService _locker;

    private static string ProxyLockKey(Proxy proxy) => "proxy_" + proxy.Id;

    public ProxyStorageService(
        Proxy[] proxies,
        ILogger<ProxyStorageService> logger,
        IServiceProvider serviceProvider,
        ITakerNodesManager takerNodesManager,
        KeyedLockService locker
    )
    {
        _proxies = proxies;
        _proxiesQueue = new ConcurrentQueue<Proxy>(_proxies);
        _logger = logger;
        _serviceProvider = serviceProvider;
        _takerNodesManager = takerNodesManager;
        _locker = locker;
        _logger.LogInformation($"Proxies were loaded in the amount of {_proxies.Length} pieces");
    }

    public ProxyDto[] GetProxies()
    {
        return _proxies.Select(x => x.ToProxyDto()).ToArray();
    }

    public async ValueTask<bool> TryReleaseProxy(int proxyId, Guid releaseKey)
    {
        var proxy = _proxies.SingleOrDefault(x => x.Id == proxyId);
        if (proxy is null || await TryRelease(proxy, releaseKey) == false)
        {
            _logger.LogWarning($"Proxy {proxyId} was not found or was not released");

            return false;
        }

        _proxiesQueue.Enqueue(proxy);

        _logger.LogInformation($"Proxy {proxy.Id} was released");
        return true;
    }

    public async ValueTask<Proxy?> TryTakeProxy(string nodeAddress)
    {
        while (!_proxiesQueue.IsEmpty)
        {
            if (!_proxiesQueue.TryDequeue(out var proxy))
                continue;

            if (!proxy.IsFree())
                throw new InvalidOperationException($"Proxy {proxy.Id} is not free, but should be");

            var releaseKey = Guid.NewGuid();

            if (await TrySetNodeToProxy(nodeAddress, releaseKey, proxy) == false)
            {
                continue;
            }

            _logger.LogInformation($"Proxy {proxy.Id} was taken");
            var heartbeatService =
                _serviceProvider.GetHostedService<ProxyHeartbeatBackgroundService>();
            if (heartbeatService is null)
            {
                _logger.LogWarning("Heartbeat service was not found");
                return null;
            }

            if (!heartbeatService.TryRegisterProxy(proxy))
            {
                _logger.LogWarning($"Proxy {proxy.Id} was not registered to heartbeat service");
                if (await TryRelease(proxy, releaseKey) == false)
                {
                    throw new InvalidOperationException(
                        $"Proxy {proxy.Id} was not released " + $"after heartbeat failure"
                    );
                }
                _proxiesQueue.Enqueue(proxy);
                return null;
            }

            return proxy;
        }

        _logger.LogWarning("All proxies were taken");
        return null;
    }

    //TODO: remove and use proxy release key in calling method
    public async ValueTask<bool> TryForceReleaseProxy(int id)
    {
        var proxy = _proxies.SingleOrDefault(x => x.Id == id);
        if (proxy is null)
        {
            throw new ArgumentNullException($"Proxy with id {id} doesnt exist");
        }

        if (await TryForceRelease(proxy))
        {
            return true;
        }

        _logger.LogInformation($"Cannot force release proxy with id {proxy.Id}");
        return false;
    }

    private async Task<bool> TryRelease(Proxy proxy, Guid releaseKey)
    {
        await _locker.LockAsync(ProxyLockKey(proxy));

        if (proxy.IsFree() || proxy.ReleaseKey is null || proxy.ReleaseKey != releaseKey)
        {
            return false;
        }

        await _takerNodesManager.RemoveByReleaseKeyKey(releaseKey);

        proxy.TakerAddress = null;
        proxy.ReleaseKey = null;
        proxy.TakenDateTime = null;

        return true;
    }

    private async Task<bool> TrySetNodeToProxy(string takerAddress, Guid releaseKey, Proxy proxy)
    {
        if (string.IsNullOrEmpty(takerAddress))
        {
            throw new ArgumentException("Taker address cannot be null or empty");
        }

        await _locker.LockAsync(ProxyLockKey(proxy));

        if (proxy.IsFree() == false)
        {
            return false;
        }

        await _takerNodesManager.Append(new NodeInfoData(takerAddress, releaseKey));

        proxy.TakerAddress = takerAddress;
        proxy.ReleaseKey = releaseKey;
        proxy.TakenDateTime = DateTime.Now;
        return true;
    }

    public async Task<bool> TryProlong(Guid releaseKey, TimeSpan prolongTime, Proxy proxy)
    {
        await _locker.LockAsync(ProxyLockKey(proxy));

        if (proxy.IsFree())
        {
            return false;
        }

        if (proxy.EqualsReleaseKey(releaseKey) == false)
        {
            return false;
        }

        proxy.TakenDateTime += prolongTime;

        return true;
    }

    private async Task<bool> TryForceRelease(Proxy proxy)
    {
        await _locker.LockAsync(ProxyLockKey(proxy));

        if (proxy.IsFree())
        {
            return false;
        }

        if (proxy.ReleaseKey is not null)
        {
            await _takerNodesManager.RemoveByReleaseKeyKey(proxy.ReleaseKey.Value);
        }

        proxy.TakerAddress = null;
        proxy.ReleaseKey = null;
        proxy.TakenDateTime = null;
        return true;
    }
}
