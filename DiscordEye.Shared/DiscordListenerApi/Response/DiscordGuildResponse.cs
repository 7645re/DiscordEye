namespace DiscordEye.Shared.DiscordListenerApi.Response;

public class DiscordGuildResponse
{
    public required string Id { get; set; }
    public required string? IconUrl { get; set; }
    public required string Name { get; set; }
    public required string OwnerId { get; set; }
    public required List<DiscordChannelResponse> Channels { get; set; } = [];
}