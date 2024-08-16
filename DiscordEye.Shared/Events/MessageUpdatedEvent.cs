namespace DiscordEye.Shared.Events;

public class MessageUpdatedEvent
{
    public required ulong MessageId { get; set; }
    public required string NewContent { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}