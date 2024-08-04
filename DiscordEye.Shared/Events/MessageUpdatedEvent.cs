namespace DiscordEye.Shared.Events;

public class MessageUpdatedEvent
{
    public required ulong GuildId { get; set; }
    public required ulong ChannelId { get; set; }
    public required ulong UserId { get; set; }
    public required ulong MessageId { get; set; }
    public required string OldContent { get; set; }
    public required string NewContent { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}