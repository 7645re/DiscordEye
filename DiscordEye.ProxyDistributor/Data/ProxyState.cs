namespace DiscordEye.ProxyDistributor.Data;

public record ProxyState(string NodeAddress, Guid ReleaseKey, DateTime LastReservationTime);