using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyDistributor;

public interface IProxyDistributorService
{
    Task<ProxyWithProxyState?> ReserveProxy(string nodeAddress);
    Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey);
}