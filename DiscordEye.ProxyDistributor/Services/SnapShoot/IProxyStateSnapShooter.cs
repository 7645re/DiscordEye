using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.SnapShoot;

public interface IProxyStateSnapShooter
{
    Task<bool> SnapShootAsync(IDictionary<Guid, ProxyState?> proxyStates);
    Task<IDictionary<Guid, ProxyState?>?> LoadSnapShotAsync();
}