namespace DiscordEye.Node;

public class StreamStartedRequest
{
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong UserId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}