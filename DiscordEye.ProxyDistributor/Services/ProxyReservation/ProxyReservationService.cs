using System.Collections.Concurrent;
using System.Text.Json;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.FileManagers;

namespace DiscordEye.ProxyDistributor.Services.ProxyReservation;

public class ProxyReservationService : IProxyReservationService
{
    private readonly IReadOnlyCollection<Proxy> _proxiesPool;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly KeyedLockService _locker;
    private readonly IProxyStateFileManager _proxyStateFileManager;
    private readonly ConcurrentDictionary<Guid, ProxyState?> _proxiesStates;
    private readonly ConcurrentDictionary<string, Guid> _reservedProxyIdByNodeAddress;
    
    public ProxyReservationService(
        KeyedLockService locker,
        Proxy[] proxiesPool,
        IProxyStateFileManager proxyStateFileManager)
    {
        _locker = locker;
        _proxiesPool = proxiesPool;
        _proxyStateFileManager = proxyStateFileManager;
        _proxiesQueue = new ConcurrentQueue<Proxy>(proxiesPool);
        _reservedProxyIdByNodeAddress = new ConcurrentDictionary<string, Guid>();
        _proxiesStates = new ConcurrentDictionary<Guid, ProxyState?>(proxiesPool
            .Select(proxy => new
            {
                proxy, state = (ProxyState?)null
            })
            .ToDictionary(x => x.proxy.Id, x => x.state));
    }

    public async Task<bool> ProlongProxy(Guid proxyId, DateTime newDateTime)
    {
        var proxy = _proxiesPool.SingleOrDefault(x => x.Id == proxyId);
        if (proxy is null)
        {
            return false;
        }

        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            if (_proxiesStates.TryGetValue(proxyId, out var proxyState) == false
                || proxyState is null)
            {
                return false;
            }

            var newProxyState = proxyState with { LastReservationTime = newDateTime };
            _proxiesStates.TryUpdate(proxy.Id, newProxyState, proxyState);
            
            await CreateProxiesStatesSnapshot();
            return true;
        }
    }
    
    public async Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey)
    {
        var proxy = _proxiesPool.SingleOrDefault(x => x.Id == proxyId);
        if (proxy is null)
        {
            return false;
        }

        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            if (_proxiesStates.TryGetValue(proxy.Id, out var proxyState) == false
                || proxyState is null
                || proxyState.ReleaseKey.Equals(releaseKey) == false)
            {
                return false;
            }

            _proxiesStates.TryUpdate(proxy.Id, null, proxyState);
            _proxiesQueue.Enqueue(proxy);
            
            await CreateProxiesStatesSnapshot();
            return true;
        }
    }

    public async Task<ProxyWithProxyState?> ReserveProxy(string nodeAddress)
    {
        while (!_proxiesQueue.IsEmpty)
        {
            if (!_proxiesQueue.TryDequeue(out var proxy))
            {
                continue;
            }

            var releaseKey = GenerateReleaseKey();
            var proxyState = await ReserveProxyInternal(proxy, nodeAddress, releaseKey);
            if (proxyState == null)
            {
                _proxiesQueue.Enqueue(proxy);
                continue;
            }
            
            return new ProxyWithProxyState(proxy, proxyState);
        }

        return null;
    }

    private async Task<ProxyState?> ReserveProxyInternal(Proxy proxy, string nodeAddress, Guid releaseKey)
    {
        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            if (_proxiesStates.TryGetValue(proxy.Id, out var existProxyState) == false
                || existProxyState is not null)
            {
                return null;
            }
    
            var proxyState = new ProxyState(
                nodeAddress,
                releaseKey,
                DateTime.Now);

            if (_proxiesStates.TryUpdate(proxy.Id, proxyState, null) == false)
            {
                return null;
            }
            
            await CreateProxiesStatesSnapshot();
            return proxyState;
        }
    }

    //TODO: need to use file manager
    private async Task CreateProxiesStatesSnapshot()
    {
        await File.WriteAllTextAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Snapshots/proxiesStates.json"),
            JsonSerializer.Serialize(_proxiesStates));
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