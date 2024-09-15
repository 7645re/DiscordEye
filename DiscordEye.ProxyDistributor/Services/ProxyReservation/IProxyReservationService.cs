using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyReservation;

public interface IProxyReservationService
{
    Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey);
    Task<ProxyWithProxyState?> ReserveProxy(string nodeAddress);
    Task<bool> ProlongProxy(Guid proxyId, DateTime newDateTime);
}