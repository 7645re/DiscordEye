namespace DiscordEye.Node.Data;

public class DiscordGuild
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required string? IconUrl { get; set; }
    public required ulong OwnerId { get; set; }
    public required List<DiscordChannel> Channels { get; set; } = [];
}