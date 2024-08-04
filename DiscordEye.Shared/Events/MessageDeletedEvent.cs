namespace DiscordEye.Shared.Events;

public class MessageDeletedEvent
{
    public required ulong GuildId { get; set; }
    public required ulong ChannelId { get; set; }
    public required ulong UserId { get; set; }
    public required ulong MessageId { get; set; }
    public required string Content { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}