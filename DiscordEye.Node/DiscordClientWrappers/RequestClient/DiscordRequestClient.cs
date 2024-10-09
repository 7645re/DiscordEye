using System.Net;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.Infrastructure.Services.Lock;
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
    private readonly KeyedLockService _keyedLockService;
    private readonly IProxyHolderService _proxyHolderService;
    private readonly ILogger<DiscordRequestClient> _logger;

    public DiscordRequestClient(
        IProxyHolderService proxyHolderService,
        ILogger<DiscordRequestClient> logger,
        KeyedLockService keyedLockService)
    {
        _proxyHolderService = proxyHolderService;
        _logger = logger;
        _keyedLockService = keyedLockService;
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
            throw new CloudFlareException();
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
        _manualReset.Wait();
        var requestTask = func();

        using (await _keyedLockService.LockAsync("requestTasks"))
        {
            _requestsTasks.Add(requestTask);
            _logger.LogInformation($"Task with ID {requestTask.Id} has been added to the list of tasks");    
        }
        
        Task.Run(async () =>
        {
            try
            {
                await requestTask;
                using (await _keyedLockService.LockAsync("requestTasks"))
                {
                    if (_requestsTasks.Remove(requestTask))
                    {
                        _logger.LogInformation($"Task with ID {requestTask.Id} was successfully removed from the task list");
                    }
                    else
                    {
                        _logger.LogCritical($"The task with ID {requestTask.Id} was not removed from the task list");
                    }
                }
                return Task.CompletedTask;
            }
            catch (CloudFlareException e)
            {
                _logger.LogWarning("CloudFlare has limited requests for this IP address");
                _manualReset.Reset();

                using (await _keyedLockService.LockAsync("requestTasks"))
                {
                    await Task.WhenAll(_requestsTasks);
                }

                var proxy = await _proxyHolderService.ReserveProxyWithRetries();
                if (proxy is null)
                {
                    return default;
                }
                _client = await InitClientAsync(proxy.ToWebProxy());
                _manualReset.Set();
                return default;
            }
        });
        
        return requestTask;
    }
}