using DiscordEye.Shared.Response;

namespace DiscordEye.EventsAggregator;

public class DiscordApiClient
{
    private readonly HttpClient _httpClient;

    public DiscordApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserResponse?> GetUserAsync(
        string host,
        long userId,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .GetFromJsonAsync<UserResponse>(
            $"{host}/discord/users/{userId}",
            cancellationToken);
    }

    public async Task<ChannelResponse?> GetChannelAsync(
        string host,
        long channelId,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .GetFromJsonAsync<ChannelResponse>(
                $"{host}/discord/channels/{channelId}",
                cancellationToken);
    }
    
    public async Task<GuildResponse?> GetGuildAsync(
        string host,
        long guildId,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .GetFromJsonAsync<GuildResponse>(
                $"{host}/discord/guilds/{guildId}",
                cancellationToken);
    }
}