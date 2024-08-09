namespace DiscordEye.Shared.Response;

public class UserResponse
{
    public required long Id { get; set; }
    public required string Username { get; set; }
    public GuildResponse[] Guilds { get; set; }
}