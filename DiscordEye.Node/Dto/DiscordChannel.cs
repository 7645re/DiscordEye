namespace DiscordEye.Node.Dto;

public class DiscordChannel
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required DiscordChannelType Type { get; set; }
}