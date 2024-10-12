namespace DiscordEye.Node.Data;

public class DiscordUser
{
    public required ulong Id { get; set; }
    public required string Username { get; set; }
    public required List<DiscordGuild> Guilds { get; set; } = [];
}