namespace DiscordEye.ProxyDistributor.Dto;


public record ProxyHeartbeat(
    Guid ProxyId,
    Guid ReleaseKey,
    string NodeAddress,
    DateTime LastHeartbeatDatetime)
{
    public bool IsDead { get; set; }
}