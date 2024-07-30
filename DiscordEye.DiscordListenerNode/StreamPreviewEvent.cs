namespace DiscordEye.DiscordListenerNode;

public class StreamPreviewEvent
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }
    public string Url { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    
    public override string ToString() => $"GuildId: {GuildId}\nChannelId: {ChannelId}\nUserId: {UserId}\nUrl: {Url}\nStartedAt: {StartedAt}";
}