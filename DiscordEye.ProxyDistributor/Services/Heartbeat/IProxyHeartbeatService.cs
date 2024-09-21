using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.Heartbeat;

public interface IProxyHeartbeatService
{
    Task<bool> RegisterProxyHeartbeat(ProxyHeartbeat proxyHeartbeat);
    Task<bool> UnRegisterProxyHeartbeat(Guid proxyId);
    Task PulseProxiesHeartbeats();
}