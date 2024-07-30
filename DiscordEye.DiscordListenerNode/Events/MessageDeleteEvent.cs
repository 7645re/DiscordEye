namespace DiscordEye.DiscordListenerNode.Events;

public class MessageDeleteEvent : BaseEvent
{
    public override EventType EventType { get; set; } = EventType.MessageDeleted;
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    public ulong UserId { get; init; }
    public ulong MessageId { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset DeletedAt { get; init; }
    
    public override string ToString() => $"GuildId: {GuildId}\n" +
                                         $"ChannelId: {ChannelId}\n" +
                                         $"UserId: {UserId}\n" +
                                         $"MessageId: {MessageId}\n" +
                                         $"Content: {Content}\n" +
                                         $"DeletedAt: {DeletedAt}";
}