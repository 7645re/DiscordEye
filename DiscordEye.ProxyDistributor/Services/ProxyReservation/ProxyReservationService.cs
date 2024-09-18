using System.Collections.Concurrent;
using System.Collections.Immutable;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.ProxyStateSnapShooter;
using DiscordEye.ProxyDistributor.Services.Vault;

namespace DiscordEye.ProxyDistributor.Services.ProxyReservation;

public class ProxyReservationService : IProxyReservationService
{
    private readonly ConcurrentDictionary<Guid, Proxy> _proxiesById;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly KeyedLockService _locker;
    private readonly ConcurrentDictionary<Guid, ProxyState?> _proxiesStates;
    private readonly ConcurrentDictionary<string, Guid> _reservedProxyIdByNodeAddress;
    private readonly IProxyStateSnapShooter _proxyStateSnapShooter;
    private readonly IProxyVaultService _proxyVaultService;
    
    public ProxyReservationService(
        KeyedLockService locker,
        IProxyVaultService proxyVaultService,
        IProxyStateSnapShooter proxyStateSnapShooter)
    {
        _locker = locker;
        _proxyVaultService = proxyVaultService;
        _proxyStateSnapShooter = proxyStateSnapShooter;
        var loadedData = LoadData().GetAwaiter().GetResult();
        _proxiesById = loadedData.proxiesById;
        _proxiesQueue = loadedData.proxiesQueue;
        _reservedProxyIdByNodeAddress = loadedData.reservedProxyIdByNodeAddress;
        _proxiesStates = loadedData.proxiesStates;
    }

    private async Task<(
        ConcurrentDictionary<Guid, Proxy> proxiesById,
        ConcurrentQueue<Proxy> proxiesQueue,
        ConcurrentDictionary<string, Guid> reservedProxyIdByNodeAddress,
        ConcurrentDictionary<Guid, ProxyState?> proxiesStates
        )> LoadData()
    {
        var proxies = (await _proxyVaultService.GetAllProxiesAsync())
            .Select(x => x.ToProxy())
            .ToImmutableArray();
        var proxiesStates = await _proxyStateSnapShooter.LoadSnapShotAsync();
        var proxiesById = new ConcurrentDictionary<Guid, Proxy>(
            proxies
                .ToDictionary(keySelector: x => x.Id, elementSelector: x => x));
        var proxiesQueue = new ConcurrentQueue<Proxy>(
            proxies.Where(x => proxiesStates[x.Id] is null));
        var reservedProxyIdByNodeAddress = new ConcurrentDictionary<string, Guid>(
            proxiesStates
            .Where(x => x.Value is not null)
            .Select(
                x => new KeyValuePair<string, Guid>(
                    key: x.Value.NodeAddress, value: x.Key)));

        return (
            proxiesById,
            proxiesQueue,
            reservedProxyIdByNodeAddress,
            new ConcurrentDictionary<Guid, ProxyState?>(proxiesStates));
    }
    
    public async Task<bool> ProlongProxy(Guid proxyId, DateTime newDateTime)
    {
        if (!_proxiesById.TryGetValue(proxyId, out var proxy))
        {
            return false;
        } 

        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            var proxyState = GetProxyState(proxyId);
            if (proxyState is null)
            {
                return false;
            }

            var newProxyState = proxyState with { LastReservationTime = newDateTime };
            _proxiesStates.TryUpdate(proxy.Id, newProxyState, proxyState);
            await _proxyStateSnapShooter.SnapShootAsync(_proxiesStates);
            
            return true;
        }
    }
    
    public async Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey)
    {
        if (!_proxiesById.TryGetValue(proxyId, out var proxy))
        {
            return false;
        } 

        using (await _locker.LockAsync(GetProxyLockKey(proxy)))
        {
            var proxyState = GetProxyState(proxy.Id);
            if (proxyState is null || proxyState.ReleaseKey.Equals(releaseKey) == false)
            {
                return false;
            }

            if (!_proxiesStates.TryUpdate(proxy.Id, null, proxyState))
            {
                return false;
            }
            await _proxyStateSnapShooter.SnapShootAsync(_proxiesStates);
            
            _reservedProxyIdByNodeAddress.TryRemove(
                new KeyValuePair<string, Guid>(
                    proxyState.NodeAddress,
                    proxyId));
            
            _proxiesQueue.Enqueue(proxy);

            return true;
        }
    }

    public async Task<ProxyWithProxyState?> ReserveProxy(string nodeAddress)
    {
        if (_reservedProxyIdByNodeAddress.TryGetValue(nodeAddress, out _))
        {
            return null;
        }
        
        while (!_proxiesQueue.IsEmpty)
        {
            if (!_proxiesQueue.TryDequeue(out var proxy))
            {
                continue;
            }

            var proxyState = await ReserveProxyInternal(
                proxy,
                nodeAddress,
                GenerateReleaseKey());

            if (proxyState == null)
            {
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
            var proxyState = GetProxyState(proxy.Id);

            if (proxyState is not null)
            {
                return null;
            }

            if (!_reservedProxyIdByNodeAddress.TryAdd(nodeAddress, proxy.Id))
            {
                return null;
            }
            
            var newProxyState = new ProxyState(
                nodeAddress,
                releaseKey,
                DateTime.Now);
            
            if (_proxiesStates.TryUpdate(proxy.Id, newProxyState, proxyState) == false)
            {
                return null;
            }
            
            await _proxyStateSnapShooter.SnapShootAsync(_proxiesStates);
            return newProxyState;
        }
    }
    
    private ProxyState? GetProxyState(Guid proxyId)
    {
        if (!_proxiesStates.TryGetValue(proxyId, out var proxyState))
        {
            throw new ArgumentException("Critical error, it should not be that there " +
                                        "is no entry in the dictionary with the proxy ID key");
        }

        return proxyState;
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