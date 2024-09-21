using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.SnapShoot;

public class ProxyStateSnapShooter(ILogger<SnapShooterBase<IDictionary<Guid, ProxyState>>> logger) 
    : SnapShooterBase<IDictionary<Guid, ProxyState?>>("ProxyStateSnapshot.json", logger), 
        IProxyStateSnapShooter;