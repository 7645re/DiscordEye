namespace DiscordEye.DiscordListenerNode;

public class DiscordMessageDeleteEvent
{
    public ulong GuildId { get; init; }
    public ulong ChannelId { get; init; }
    public ulong UserId { get; init; }
    public ulong MessageId { get; init; }
    public string Content { get; init; }
    public DateTimeOffset DeletedAt { get; init; }
    
    public override string ToString() => $"GuildId: {GuildId}\nChannelId: {ChannelId}\nUserId: {UserId}\nMessageId: {MessageId}\nContent: {Content}\nDeletedAt: {DeletedAt}";
}