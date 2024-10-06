using System.Collections.Concurrent;
using System.Net;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.Node.Data;
using DiscordEye.Node.Mappers;
using DiscordEye.Node.Services.ProxyHolder;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.Node.DiscordClientWrappers.RequestClient;

public class DiscordRequestClient : IDiscordRequestClient
{
    private DiscordSocketClient? _client;
    private readonly string _token;
    private readonly ManualResetEventSlim _manualReset = new(true);
    private readonly ConcurrentBag<Task> _requestsTasks = new();
    private readonly IProxyHolderService _proxyHolderService;
    private readonly ILogger<DiscordRequestClient> _logger;

    public DiscordRequestClient(
        IProxyHolderService proxyHolderService,
        ILogger<DiscordRequestClient> logger)
    {
        _proxyHolderService = proxyHolderService;
        _logger = logger;
        _token = StartupExtensions.GetDiscordTokenFromEnvironment();
        var proxy = _proxyHolderService.GetCurrentHoldProxy().GetAwaiter().GetResult();
        if (proxy is null)
        {
            throw new NullReferenceException("Node can't work without a proxy");
        }
        
        _client = InitClientAsync(proxy.ToWebProxy()).GetAwaiter().GetResult();
    }

    private async Task<DiscordSocketClient> InitClientAsync(WebProxy? webProxy = null)
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        if (webProxy is not null)
        {
            discordSocketConfig.RestClientProvider = DefaultRestClientProvider.Create(
                webProxy: webProxy,
                useProxy: true);
            _logger.LogInformation($"Client will be launched using a proxy {webProxy.Address}");
            Environment.Exit(1);
        }

        var client = new DiscordSocketClient(discordSocketConfig);
        await client.LoginAsync(TokenType.User, _token);
        await client.StartAsync();
        _logger.LogInformation("Client has been fully launched");
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
        return await ExecuteRequest(async () =>
        {
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
        });
    }

    private async Task<T?> ExecuteRequest<T>(Func<Task<T>> func)
    {
        _manualReset.Wait();
        var requestTask = func();
        _requestsTasks.Add(requestTask);
        try
        {
            var result = await requestTask;
            return result;
        }
        catch (CloudFlareException e)
        {
            _logger.LogWarning("CloudFlare has limited requests for this IP address");
            _manualReset.Reset();
            await Task.WhenAll(_requestsTasks);
            var proxy = await _proxyHolderService.ReserveProxyWithRetries();
            if (proxy is null)
            {
                return default;
            }
            _client = await InitClientAsync(proxy.ToWebProxy());
            _manualReset.Set();
            return default;
        }
    }
}