namespace DiscordEye.Shared.Events;

public class MessageDeletedEvent
{
    public required int NodeId { get; set; }
    public required long MessageId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}