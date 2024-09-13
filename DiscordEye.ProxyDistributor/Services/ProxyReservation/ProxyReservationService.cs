using System.Collections.Concurrent;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.FileManagers;

namespace DiscordEye.ProxyDistributor.Services.ProxyReservation;

public class ProxyReservationService : IProxyReservationService
{
    private readonly IReadOnlyCollection<Proxy> _proxies;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly KeyedLockService _locker;
    private readonly IProxyStateFileManager _proxyStateFileManager;
    private readonly ConcurrentDictionary<Guid, ProxyState?> _proxiesStates;
    
    public ProxyReservationService(
        KeyedLockService locker,
        Proxy[] proxies,
        IProxyStateFileManager proxyStateFileManager)
    {
        _locker = locker;
        _proxies = proxies;
        _proxyStateFileManager = proxyStateFileManager;
        _proxiesQueue = new ConcurrentQueue<Proxy>(proxies);
        _proxiesStates = new ConcurrentDictionary<Guid, ProxyState?>(proxies
            .Select(proxy => new
            {
                proxy, state = (ProxyState?)null
            })
            .ToDictionary(x => x.proxy.Id, x => x.state));
    }

    public IReadOnlyCollection<Proxy> GetProxies()
    {
        return _proxies;
    }

    public async Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey)
    {
        var proxy = _proxies.SingleOrDefault(x => x.Id == proxyId);
        if (proxy is null)
        {
            return false;
        }

        if (!await ReleaseProxyInternal(proxy, releaseKey))
        {
            return false;
        }
        
        return true;
    }

    private async Task<bool> ReleaseProxyInternal(Proxy proxy, Guid releaseKey)
    {
        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            var proxyState = GetProxyState(proxy.Id);

            if (ProxyIsFree(proxy.Id) || proxyState is null || proxyState.ReleaseKey.Equals(releaseKey) == false)
            {
                return false;
            }

            RemoveProxyState(proxy.Id);
            
            await _proxyStateFileManager.RemoveByReleaseKey(releaseKey);
            _proxiesQueue.Enqueue(proxy);
            
            return true;
        }
    }
    
    public async Task<Proxy?> ReserveProxy(string nodeAddress)
    {
        while (!_proxiesQueue.IsEmpty)
        {
            if (!_proxiesQueue.TryDequeue(out var proxy))
            {
                continue;
            }

            var releaseKey = GenerateReleaseKey();
            if (!await ReserveProxyInternal(proxy, nodeAddress, releaseKey))
            {
                _proxiesQueue.Enqueue(proxy);
                continue;
            }

            return proxy;
        }

        return null;
    }

    private async Task<bool> ReserveProxyInternal(Proxy proxy, string nodeAddress, Guid releaseKey)
    {
        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            if (!ProxyIsFree(proxy.Id))
            {
                return false;
            }

            var reservationTime = DateTime.Now;
            var updatedProxyState = new ProxyState(
                nodeAddress,
                releaseKey,
                reservationTime);

            await _proxyStateFileManager.Append(updatedProxyState);
            
            _proxiesStates.AddOrUpdate(
                proxy.Id,
                updatedProxyState,
                (_, _) => updatedProxyState
            );
            
            return true;
        }
    }

    private bool ProxyIsFree(Guid proxyId)
    {
        return GetProxyState(proxyId) is not null;
    }

    private ProxyState? GetProxyState(Guid proxyId)
    {
        if (!_proxiesStates.TryGetValue(proxyId, out var proxyState))
            throw new ArgumentException("Doesn't have proxy state");

        return proxyState;
    }

    private void RemoveProxyState(Guid proxyId)
    {
        _proxiesStates.AddOrUpdate(proxyId, _ => null, (_, _) => null);
    }
    
    private static string GetProxyLockKey(Proxy proxy)
    {
        return "proxy_" + proxy.Id;
    }
    
    private static Guid GenerateReleaseKey()
    {
        return Guid.NewGuid();
    }
}