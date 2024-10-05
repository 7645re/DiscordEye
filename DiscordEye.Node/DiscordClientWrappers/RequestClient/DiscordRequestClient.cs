using System.Collections.Concurrent;
using System.Net;
using Discord;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.Node.Data;
using DiscordEye.Node.Mappers;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.Node.DiscordClientWrappers.RequestClient;

public class DiscordRequestClient : IDiscordRequestClient
{
    private readonly DiscordSocketClient? _client;
    private readonly string _token;
    private readonly ManualResetEventSlim _manualReset = new(true);
    private readonly ConcurrentBag<Task> _requestsTasks = new();

    public DiscordRequestClient()
    {
        _token = StartupExtensions.GetDiscordTokenFromEnvironment();
        _client = InitClientAsync().GetAwaiter().GetResult();
    }

    private async Task<DiscordSocketClient> InitClientAsync(WebProxy? webProxy = null)
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        if (webProxy is not null)
            discordSocketConfig.RestClientProvider = DefaultRestClientProvider.Create(
                webProxy: webProxy,
                useProxy: true);

        var client = new DiscordSocketClient(discordSocketConfig);
        await client.LoginAsync(TokenType.User, _token);
        await client.StartAsync();
        return client;
    }
    
    public async Task<DiscordUser?> GetUserAsync(ulong id)
    {
        return await ExecuteRequest(async () =>
        {
            var userProfile = await _client.Rest.GetUserProfileAsync(id);
            return userProfile?.ToDiscordUser();
        });
    }

    public async Task<DiscordGuild?> GetGuildAsync(
        ulong id,
        bool withChannels = false)
    {
        _manualReset.Wait();
        var guild = await _client?.Rest.GetGuildAsync(id);
        if (guild is null)
        {
            return null;
        }

        var channels = withChannels switch
        {
            true => new List<RestGuildChannel>((await guild.GetChannelsAsync()).ToArray()),
            _ => new List<RestGuildChannel>(Array.Empty<RestGuildChannel>())
        };

        return guild.ToDiscordGuild(channels);
    }

    private async Task<T> ExecuteRequest<T>(Func<Task<T>> func)
    {
        _manualReset.Wait();
        var requestTask = func();
        _requestsTasks.Add(requestTask);
        return await requestTask;
    }
}