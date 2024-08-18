using System.Text.Json.Serialization;

namespace DiscordEye.Shared.Response;

public class ChannelResponse
{
    [JsonPropertyName("id")]
    public required long Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}