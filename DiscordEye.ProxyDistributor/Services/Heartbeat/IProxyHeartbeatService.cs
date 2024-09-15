using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.Heartbeat;

public interface IProxyHeartbeatService
{
    bool RegisterProxyHeartbeat(ProxyHeartbeat proxyHeartbeat);
    bool UnRegisterProxyHeartbeat(Guid proxyId);
    Task PulseProxiesHeartbeats();
}