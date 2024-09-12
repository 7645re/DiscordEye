namespace DiscordEye.ProxyDistributor.Data;

public record ProxyState(Guid ProxyId, string? NodeAddress, Guid? ReleaseKey, DateTime? LastReservationTime);