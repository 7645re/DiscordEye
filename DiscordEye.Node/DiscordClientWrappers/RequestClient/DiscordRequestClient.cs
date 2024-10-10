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

        if (_client is not null)
        {
            await _client.DisposeAsync();
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

    private async Task<Task<T?>> ExecuteRequest<T>(Func<Task<T>> func)
    {
        _manualReset.Wait();
        var requestTask = func();
        var proxyUsedWhenStartRequest = await _proxyHolderService.GetCurrentHoldProxy();
        ThrowIfProxyIsNull(proxyUsedWhenStartRequest);

        await ModifyRequestsTasksInLock(requestsTask =>
        {
            requestsTask.Add(requestTask);
            _logger.LogInformation($"Task with ID {requestTask.Id} has been added to the list of tasks");
        });
        
        Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation($"The task with ID {requestTask.Id} has started waiting for completion");
                await requestTask;
                await ModifyRequestsTasksInLock(requestsTask =>
                {
                    if (requestsTask.Remove(requestTask))
                    {
                        _logger.LogInformation(
                            $"Task with ID {requestTask.Id} was successfully removed from the task list");
                    }
                    else
                    {
                        _logger.LogCritical($"The task with ID {requestTask.Id} was not removed from the task list");
                    }
                });
            }
            catch (CloudFlareException e)
            {
                using (await _keyedLockService.LockAsync("ProcessingCloudFlareException"))
                {
                    var currentProxy = await _proxyHolderService.GetCurrentHoldProxy();
                    ThrowIfProxyIsNull(currentProxy);

                    if (proxyUsedWhenStartRequest.ReleaseKey != currentProxy.ReleaseKey)
                    {
                        _logger.LogInformation($"CloudFlare error occurred in a task that was running with" +
                                               $" the release key {proxyUsedWhenStartRequest.ReleaseKey}, a new proxy" +
                                               $" with the key {currentProxy.ReleaseKey} had already been requested");
                        await ModifyRequestsTasksInLock(requestsTasks =>
                        {
                            requestsTasks.Remove(requestTask);
                            _logger.LogInformation($"The task with ID {requestTask.Id} has been removed" +
                                                   $" from the request task list");
                        });
                        return;
                    }

                    _logger.LogWarning($"CloudFlare has limited requests in task with ID {requestTask.Id}" +
                                       $" for proxy with release key {proxyUsedWhenStartRequest.ReleaseKey}");
                    _manualReset.Reset();
                    _logger.LogWarning("The ability to add new tasks to requests has been suspended");

                    _logger.LogInformation($"Started waiting for tasks to complete with IDs: " +
                                           $"{string.Join(", ", _requestsTasks.Select(x => x.Id))}");

                    await TaskWhenAllInTryCatch(_requestsTasks.ToArray());
                    _logger.LogWarning("Waited for all requests task to complete");
                    
                    await ModifyRequestsTasksInLock(requestsTask =>
                    {
                        requestsTask.Clear();
                        _logger.LogWarning("Cleared the list of tasks for requests");
                    });

                    var proxy = await _proxyHolderService.ReserveProxyWithRetries();
                    if (proxy is null)
                    {
                        _logger.LogWarning("There is no way to reinitialize the client " +
                                           "because the proxy distributor was unable to reserve a proxy");
                        return;
                    }

                    _client = await InitClientAsync(proxy.ToWebProxy());
                    _manualReset.Set();
                    _logger.LogWarning("The ability to add new tasks to requests has been restored");
                }
            }
        });
        
        return Task.Run(async () =>
        {
            try
            {
                return await requestTask;
            }
            catch (CloudFlareException e)
            {
            }

            return default;
        });
    }

    private void ThrowIfProxyIsNull(Proxy? proxy)
    {
        if (proxy is not null) return;
        _logger.LogCritical("Unexpected application behavior, client should not be used without a proxy");
        throw new NullReferenceException("Unexpected application behavior, client should" +
                                         " not be used without a proxy");
    }

    private async Task<(Task task, Exception? exception)> TaskWhenAllInTryCatch(Task[] tasks)
    {
        Task? t = null;
        try
        {
            t = Task.WhenAll(tasks);
            await t;
            return (t, null);
        }
        catch (Exception ex)
        {
            return (t!, ex);
        }
    }

    private async Task ModifyRequestsTasksInLock(Action<List<Task>> modifyAction)
    {
        using (await _keyedLockService.LockAsync("requestTasks"))
        {
            modifyAction(_requestsTasks);
        }
    }
}