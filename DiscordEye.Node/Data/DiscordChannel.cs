namespace DiscordEye.Node.Data;

public class DiscordChannel
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required DiscordChannelType Type { get; set; }
}