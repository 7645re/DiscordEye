using System.Text.Json.Serialization;

namespace DiscordEye.Shared.Response;

public class UserResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("username")]
    public required string Username { get; set; }
    [JsonPropertyName("guilds")]
    public GuildResponse[] Guilds { get; set; }
}