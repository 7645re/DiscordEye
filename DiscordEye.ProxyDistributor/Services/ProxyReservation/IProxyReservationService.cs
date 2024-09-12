using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.ProxyReservation;

public interface IProxyReservationService
{
    IReadOnlyCollection<Proxy> GetProxies();
    Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey);
    Task<Proxy?> ReserveProxy(string nodeAddress);
}