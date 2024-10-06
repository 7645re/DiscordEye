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
    private readonly List<Task> _requestsTasks = new();
    private readonly SemaphoreSlim _requestTasksSemaphoreSlim = new(1,1);
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
            _logger.LogCritical("Node can't work without a proxy");
            Environment.Exit(1);
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
        }

        var client = new DiscordSocketClient(discordSocketConfig);
        await client.LoginAsync(TokenType.User, _token);
        await client.StartAsync();
        _logger.LogInformation("Client has been fully launched");
        return client;
    }
    
    public async Task<DiscordUser?> GetUserAsync(ulong id)
    {
        return await await ExecuteRequest(async () =>
        {
            await Task.Delay(10000);
            var userProfile = await _client.Rest.GetUserProfileAsync(id);
            return userProfile?.ToDiscordUser();
        });
    }

    public async Task<DiscordGuild?> GetGuildAsync(
        ulong id,
        bool withChannels = false)
    {
        return await await ExecuteRequest(async () =>
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

    private async Task<Task<T>?> ExecuteRequest<T>(Func<Task<T>> func)
    {
        try
        {
            _manualReset.Wait();
            var requestTask = func();
            await _requestTasksSemaphoreSlim.WaitAsync();
            _requestsTasks.Add(requestTask);
            _logger.LogInformation($"{DateTime.Now} Таска {requestTask.Id} добавлена в список тасок ");
            _requestTasksSemaphoreSlim.Release();

            Task.Run(() =>
            {
                requestTask.ContinueWith(_ =>
                {
                    _requestTasksSemaphoreSlim.WaitAsync();
                    _requestsTasks.Remove(requestTask);
                    _logger.LogInformation($"{DateTime.Now} Таска {requestTask.Id} удалена из списка тасок ");
                    _requestTasksSemaphoreSlim.Release();
                });
                return Task.CompletedTask;
            });
            
            return requestTask;
        }
        catch (CloudFlareException e)
        {
            _logger.LogWarning("CloudFlare has limited requests for this IP address");
            _manualReset.Reset();

            await _requestTasksSemaphoreSlim.WaitAsync();
            await Task.WhenAll(_requestsTasks);
            _requestTasksSemaphoreSlim.Release();

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