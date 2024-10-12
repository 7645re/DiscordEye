using DiscordEye.Infrastructure.Services.SnapShoot;
using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.SnapShoot;

public class ProxyHeartbeatSnapShooter(ILogger<SnapShooterBase<IDictionary<Guid, ProxyHeartbeat>>> logger)
    : SnapShooterBase<IDictionary<Guid, ProxyHeartbeat>>("ProxyHeartbeatSnapshot.json", logger), 
        IProxyHeartbeatSnapShooter;