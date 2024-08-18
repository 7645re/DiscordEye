using System.Text.Json.Serialization;

namespace DiscordEye.Shared.Response;

public class GuildResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("iconUrl")]
    public string? IconUrl { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}