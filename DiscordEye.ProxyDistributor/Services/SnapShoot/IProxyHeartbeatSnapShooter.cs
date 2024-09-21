using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.SnapShoot;

public interface IProxyHeartbeatSnapShooter
{
    Task<bool> SnapShootAsync(IDictionary<Guid,ProxyHeartbeat> heartbeats);
    Task<IDictionary<Guid,ProxyHeartbeat>?> LoadSnapShotAsync();
}