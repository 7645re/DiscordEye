namespace DiscordEye.Shared.Events;

public class MessageDeletedEvent
{
    public required ulong MessageId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}