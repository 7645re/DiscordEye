namespace DiscordEye.DiscordListenerNode.Events;

public class MessageDeletedEvent
{
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    public ulong UserId { get; init; }
    public ulong MessageId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public override string ToString() => $"GuildId: {GuildId}\n" +
                                         $"ChannelId: {ChannelId}\n" +
                                         $"UserId: {UserId}\n" +
                                         $"MessageId: {MessageId}\n" +
                                         $"Content: {Content}\n" +
                                         $"Timestamp: {Timestamp}";
}