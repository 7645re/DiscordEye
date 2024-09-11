using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public interface IProxyStorageService
{
    ValueTask<bool> TryReleaseProxy(int proxyId, Guid releaseKey);
    ValueTask<Proxy?> TryTakeProxy(string nodeAddress);
    ValueTask<bool> TryForceReleaseProxy(int id);
    ProxyDto[] GetProxies();
    Task<bool> TryProlong(Guid releaseKey, TimeSpan prolongTime, Proxy proxy);
}
