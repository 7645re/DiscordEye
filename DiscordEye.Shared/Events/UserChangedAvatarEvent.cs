namespace DiscordEye.Shared.Events;

public class UserChangedAvatarEvent
{
    public required long UserId { get; set; }
    public required string OldAvatarUrl { get; set; }
    public required string NewAvatarUrl { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}