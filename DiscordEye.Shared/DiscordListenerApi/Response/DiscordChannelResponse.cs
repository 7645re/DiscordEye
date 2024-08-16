namespace DiscordEye.Shared.DiscordListenerApi.Response;

public class DiscordChannelResponse
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DiscordChannelTypeResponse Type { get; set; }
}