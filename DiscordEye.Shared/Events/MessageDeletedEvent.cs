namespace DiscordEye.Shared.Events;

public class MessageDeletedEvent
{
    public required long MessageId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}