using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.Heartbeat;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;

namespace DiscordEye.ProxyDistributor.Services.ProxyDistributor;

public class ProxyDistributorService : IProxyDistributorService
{
    private readonly IProxyReservationService _proxyReservationService;
    private readonly IProxyHeartbeatService _proxyHeartbeatService;

    public ProxyDistributorService(
        IProxyReservationService proxyReservationService,
        IProxyHeartbeatService proxyHeartbeatService)
    {
        _proxyReservationService = proxyReservationService;
        _proxyHeartbeatService = proxyHeartbeatService;
    }
    
    public async Task<ProxyWithProxyState?> ReserveProxy(string nodeAddress)
    {
        var proxyWithProxyState = await _proxyReservationService.ReserveProxy(nodeAddress);
        if (proxyWithProxyState == null)
        {
            return null;
        }

        var proxyHeartbeat = proxyWithProxyState.ToProxyHeartbeat();
        if (await _proxyHeartbeatService.RegisterProxyHeartbeat(proxyHeartbeat) == false)
        {
            await _proxyReservationService.ReleaseProxy(
                    proxyWithProxyState.Proxy.Id,
                    proxyWithProxyState.ProxyState.ReleaseKey);
            return null;
        }

        return proxyWithProxyState;
    }

    public async Task<bool> ReleaseProxy(Guid proxyId, Guid releaseKey)
    {
        if (await _proxyReservationService.ReleaseProxy(proxyId, releaseKey) == false)
        {
            return false;
        }

        await _proxyHeartbeatService.UnRegisterProxyHeartbeat(proxyId);
        return true;
    }
}