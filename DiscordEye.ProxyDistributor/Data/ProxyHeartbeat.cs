namespace DiscordEye.ProxyDistributor.Data;


public record ProxyHeartbeat(
    Guid ProxyId,
    Guid ReleaseKey,
    string NodeAddress,
    DateTime LastHeartbeatDatetime);