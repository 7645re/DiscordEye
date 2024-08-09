namespace DiscordEye.Shared.Response;

public class GuildResponse
{
    public required long Id { get; set; }
    public string? IconUrl { get; set; }
    public string? Name { get; set; }
}