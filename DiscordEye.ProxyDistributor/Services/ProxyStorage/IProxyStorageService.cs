using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public interface IProxyStorageService
{
    bool TryReleaseProxy(int proxyId, Guid releaseKey);
    bool TryTakeProxy(out (Proxy takenProxy, Guid releaseKey)? takenProxyWithKey);
    ProxyInfo[] GetProxies();
}