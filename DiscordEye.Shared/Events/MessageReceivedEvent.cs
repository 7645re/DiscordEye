namespace DiscordEye.Shared.Events;

public class MessageReceivedEvent
{
    public required long GuildId { get; set; }
    public required long ChannelId { get; set; }
    public required long UserId { get; set; }
    public required long MessageId { get; set; }
    public required string Content { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}