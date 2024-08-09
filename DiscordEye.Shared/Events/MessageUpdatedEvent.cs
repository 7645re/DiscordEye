namespace DiscordEye.Shared.Events;

public class MessageUpdatedEvent
{
    public required long MessageId { get; set; }
    public required string NewContent { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}