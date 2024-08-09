namespace DiscordEye.Shared.Events;

public class UserBannedEvent
{
    public required long GuildId { get; set; }
    public required long UserId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}