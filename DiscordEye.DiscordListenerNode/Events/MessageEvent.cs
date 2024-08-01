namespace DiscordEye.DiscordListenerNode.Events;

public class MessageEvent
{
    public required EventType EventType { get; init; }
    public required ulong GuildId { get; init; }
    public required ulong ChannelId { get; init; }
    public required ulong UserId { get; init; }
    public required ulong MessageId { get; init; }
    public required string Content { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }

    public override string ToString() => $"GuildId: {GuildId}\n" +
                                         $"ChannelId: {ChannelId}\n" +
                                         $"UserId: {UserId}\n" +
                                         $"MessageId: {MessageId}\n" +
                                         $"Content: {Content}\n" +
                                         $"Timestamp: {Timestamp}";
}