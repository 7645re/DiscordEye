namespace DiscordEye.Shared.DiscordListenerApi.Response;

public class DiscordUserResponse
{
    public required ulong Id { get; set; }
    public required string Username { get; set; }
    public List<DiscordGuildResponse> Guilds { get; set; } = [];
}