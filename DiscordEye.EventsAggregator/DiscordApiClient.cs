using DiscordEye.Shared.DiscordListenerApi.Response;

namespace DiscordEye.EventsAggregator;

public class DiscordApiClient
{
    private readonly HttpClient _httpClient;

    public DiscordApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DiscordUserResponse?> GetUserAsync(
        string host,
        ulong userId,
        bool guildInfo = false,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<DiscordUserResponse>(
            $"{host}/discord/users/{userId}?guildInfo={guildInfo}",
            cancellationToken);
    }

    public async Task<DiscordChannelResponse?> GetChannelAsync(
        string host,
        ulong channelId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<DiscordChannelResponse>(
                $"{host}/discord/channels/{channelId}",
                cancellationToken);
    }

    public async Task<IEnumerable<DiscordChannelResponse>?> GetGuildChannelsAsync(
        string host,
        long guildId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<IEnumerable<DiscordChannelResponse>>(
                $"{host}/discord/guilds/{guildId}/channels",
                cancellationToken);
    }
    
    public async Task<DiscordGuildResponse?> GetGuildAsync(
        string host,
        ulong guildId,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .GetFromJsonAsync<DiscordGuildResponse>(
                $"{host}/discord/guilds/{guildId}",
                cancellationToken);
    }
}