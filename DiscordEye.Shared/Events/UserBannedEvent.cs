namespace DiscordEye.Shared.Events;

public class UserBannedEvent
{
    public required ulong GuildId { get; set; }
    public required ulong UserId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}