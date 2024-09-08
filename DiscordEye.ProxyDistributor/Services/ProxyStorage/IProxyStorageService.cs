using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyStorage;

public interface IProxyStorageService
{
    bool TryReleaseProxy(int proxyId, Guid releaseKey);
    bool TryTakeProxy(string nodeAddress, out ( Proxy takenProxy, Guid releaseKey)? takenProxyWithKey);
    bool TryForceReleaseProxy(int id);
    ProxyInfo[] GetProxies();
}