using System.Collections.Concurrent;
using System.Collections.Immutable;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.SnapShoot;
using DiscordEye.ProxyDistributor.Services.Vault;

namespace DiscordEye.ProxyDistributor.Services.ProxyReservation;

public class ProxyReservationService : IProxyReservationService
{
    private readonly ILogger<ProxyReservationService> _logger;
    private readonly ConcurrentDictionary<Guid, Proxy> _proxiesById;
    private readonly ConcurrentQueue<Proxy> _proxiesQueue;
    private readonly KeyedLockService _locker;
    private readonly ConcurrentDictionary<Guid, ProxyState?> _proxiesStates;
    private readonly ConcurrentDictionary<string, Guid> _reservedProxyIdByNodeAddress;
    private readonly IProxyStateSnapShooter _snapShooter;
    private readonly IProxyVaultService _proxyVaultService;
    
    public ProxyReservationService(
        KeyedLockService locker,
        IProxyVaultService proxyVaultService,
        IProxyStateSnapShooter snapShooter,
        ILogger<ProxyReservationService> logger)
    {
        _locker = locker;
        _proxyVaultService = proxyVaultService;
        _snapShooter = snapShooter;
        _logger = logger;
        var loadedData = LoadData().GetAwaiter().GetResult();
        _proxiesById = loadedData.proxiesById;
        _proxiesQueue = loadedData.proxiesQueue;
        _reservedProxyIdByNodeAddress = loadedData.reservedProxyIdByNodeAddress;
        _proxiesStates = loadedData.proxiesStates;
        _logger.LogInformation($"Loaded {_proxiesById.Count} proxies and {_proxiesQueue.Count} proxies in queue");
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
        var proxiesStates = await _snapShooter.LoadSnapShotAsync()
            ?? proxies.ToDictionary(keySelector: x => x.Id, elementSelector: x => (ProxyState?)null);
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
        using (await _locker.LockAsync(GetProxyLockKey(proxyId)))
        {
            _logger.LogInformation($"Prolonging proxy {proxyId} to {newDateTime}");
            if (!_proxiesById.TryGetValue(proxyId, out var proxy))
            {
                _logger.LogWarning($"Proxy {proxyId} not found");
                return false;
            } 

            var proxyState = GetProxyState(proxyId);
            if (proxyState is null)
            {
                _logger.LogWarning($"Proxy state {proxyId} not found");
                return false;
            }

            var newProxyState = proxyState with { LastReservationTime = newDateTime };
            _proxiesStates.TryUpdate(proxy.Id, newProxyState, proxyState);
            _logger.LogInformation($"Proxy {proxyId} prolonged to {newDateTime}");
            await _snapShooter.SnapShootAsync(_proxiesStates);
        
            return true;
        }
    }
    
    public async Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey)
    {
        using (await _locker.LockAsync(GetProxyLockKey(proxyId)))
        {
            if (!_proxiesById.TryGetValue(proxyId, out var proxy))
            {
                _logger.LogWarning($"Proxy {proxyId} not found");
                return false;
            } 

            var proxyState = GetProxyState(proxy.Id);
            if (proxyState is null)
            {
                _logger.LogWarning($"Proxy {proxyId} not reserved");
                return false;
            }
        
            if (proxyState.ReleaseKey.Equals(releaseKey) == false)
            {
                _logger.LogWarning($"Release key for proxy {proxyId} not valid");
                return false;
            }

            if (!_proxiesStates.TryUpdate(proxy.Id, null, proxyState))
            {
                _logger.LogWarning($"Attempt to release proxy {proxyId} failed");
                return false;
            }
            await _snapShooter.SnapShootAsync(_proxiesStates);
        
            _reservedProxyIdByNodeAddress.TryRemove(
                new KeyValuePair<string, Guid>(
                    proxyState.NodeAddress,
                    proxyId));
        
            _proxiesQueue.Enqueue(proxy);
            _logger.LogInformation($"Proxy {proxyId} released");
            return true;
        }
    }

    public async Task<ProxyWithProxyState?> ReserveProxy(string nodeAddress)
    {
        if (_reservedProxyIdByNodeAddress.TryGetValue(nodeAddress, out _))
        {
            _logger.LogWarning($"Proxy already reserved for {nodeAddress}");
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
        using (await _locker.LockAsync(GetProxyLockKey(proxy.Id)))
        {
            var proxyState = GetProxyState(proxy.Id);

            if (proxyState is not null)
            {
                _logger.LogWarning($"Proxy {proxy.Id} already reserved");
                return null;
            }

            if (!_reservedProxyIdByNodeAddress.TryAdd(nodeAddress, proxy.Id))
            {
                _logger.LogWarning($"Error while binding proxy {proxy.Id} to {nodeAddress}");
                return null;
            }
        
            var newProxyState = new ProxyState(
                nodeAddress,
                releaseKey,
                DateTime.Now);
        
            if (_proxiesStates.TryUpdate(proxy.Id, newProxyState, proxyState) == false)
            {
                _logger.LogWarning($"Error while updating proxy state with ID {proxy.Id}");
                return null;
            }
        
            await _snapShooter.SnapShootAsync(_proxiesStates);
            _logger.LogInformation($"Proxy {proxy.Id} reserved for {nodeAddress}");
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
    
    private static string GetProxyLockKey(Guid guidId)
    {
        return "proxy_" + guidId;
    }
    
    private static Guid GenerateReleaseKey()
    {
        return Guid.NewGuid();
    }
}