namespace DiscordEye.DiscordListenerNode.Events;

public class StreamEvent
{
    public required EventType EventType { get; init; }
    public required ulong GuildId { get; set; }
    public required ulong ChannelId { get; set; }
    public required ulong UserId { get; set; }
    public string Url { get; set; } = string.Empty;
    public required DateTimeOffset Timestamp { get; set; }
    
    public override string ToString() => $"{EventType.ToString()}\n" +
                                         $"GuildId: {GuildId}\n" +
                                         $"ChannelId: {ChannelId}\n" +
                                         $"UserId: {UserId}\n" +
                                         $"Timestamp: {Timestamp}";
}