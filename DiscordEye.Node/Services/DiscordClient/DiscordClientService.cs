using System.Net;
using System.Threading.Channels;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.Node.Data;
using DiscordEye.Node.Helpers;
using DiscordEye.Node.Mappers;
using DiscordEye.Node.Options;
using DiscordEye.Node.Services.ProxyHolder;
using DiscordEye.Shared.Events;
using DiscordEye.Shared.Extensions;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DiscordEye.Node.Services.DiscordClient;

public class DiscordClientService : IDisposable, IDiscordClientService
{
    private readonly string _token;
    private DiscordSocketClient? _client;
    private readonly DiscordOptions _options;
    private readonly List<Task> _requestsTasks = [];
    private readonly IServiceProvider _serviceProvider;
    private readonly KeyedLockService _keyedLockService;
    private readonly ILogger<DiscordClientService> _logger;
    private readonly IProxyHolderService _proxyHolderService;
    private readonly ManualResetEventSlim _manualReset = new(true);
    private readonly Channel<StreamStartedRequest> _streamStartedRequestChannel;

    public DiscordClientService(
        IOptions<DiscordOptions> discordOptions,
        IProxyHolderService proxyHolderService,
        ILogger<DiscordClientService> logger,
        KeyedLockService keyedLockService,
        IServiceProvider serviceProvider,
        Channel<StreamStartedRequest> streamStartedRequestChannel)
    {
        _options = discordOptions.Value;
        _proxyHolderService = proxyHolderService;
        _logger = logger;
        _keyedLockService = keyedLockService;
        _serviceProvider = serviceProvider;
        _streamStartedRequestChannel = streamStartedRequestChannel;
        _token = StartupExtensions.GetDiscordTokenFromEnvironment();
        var proxy = _proxyHolderService.GetCurrentHoldProxy().GetAwaiter().GetResult();
        if (proxy is null)
        {            
            _logger.LogCritical("Node can't work without a proxy");
            Environment.Exit(1);
        }
        
        _client = InitClientAsync(proxy.ToWebProxy()).GetAwaiter().GetResult();
    }

    private void RegisterEventsHandlers(DiscordSocketClient socketClient)
    {
        socketClient.Ready += OnClientOnReady;
        socketClient.UserUpdated += OnClientOnUserUpdated;
        socketClient.GuildMemberUpdated += OnClientOnGuildMemberUpdated;
        socketClient.UserBanned += OnClientOnUserBanned;
        socketClient.UserVoiceStateUpdated += OnClientOnUserVoiceStateUpdated;
        socketClient.MessageDeleted += OnClientOnMessageDeleted;
        socketClient.MessageReceived += OnClientOnMessageReceived;
        socketClient.MessageUpdated += OnClientOnMessageUpdated;
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
        RegisterEventsHandlers(client);
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

    private async Task OnClientOnReady()
    {
        foreach (var guild in _client.Guilds)
        {
            await _client.SubscribeToGuildEvents(guild.Id);
        }
    }

    private async Task OnClientOnUserUpdated(SocketUser userBefore, SocketUser userAfter)
    {
        if (userBefore.GetAvatarUrl() != userAfter.GetAvatarUrl())
        {
            var eventMessage = new UserChangedAvatarEvent
            {
                UserId = userBefore.Id, OldAvatarUrl = userBefore.GetAvatarUrl(),
                NewAvatarUrl = userAfter.GetAvatarUrl(), Timestamp = DateTimeOffset.Now
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        }
    }

    private async Task OnClientOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> cacheable, SocketGuildUser user)
    {
        var before = await cacheable.GetOrDownloadAsync();

        if (before.Nickname != user.Nickname)
        {
            var eventMessage = new UserGuildChangedNicknameEvent
            {
                GuildId = before.Guild.Id,
                UserId = user.Id,
                OldUsername = before.Nickname,
                NewUsername = user.Nickname,
                Timestamp = DateTimeOffset.Now
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        }
    }

    private async Task OnClientOnUserBanned(SocketUser user, SocketGuild guild)
    {
        var eventMessage = new UserBannedEvent { GuildId = guild.Id, UserId = user.Id, Timestamp = DateTimeOffset.Now };

        await ProduceEventAsync(_serviceProvider, eventMessage);
    }

    private async Task OnClientOnUserVoiceStateUpdated(SocketUser user, SocketVoiceState voiceStateBefore,
        SocketVoiceState voiceStateAfter)
    {
        var eventType = DiscordHelper.DetermineEventType(voiceStateBefore, voiceStateAfter);

        if (eventType == UserVoiceChannelActionType.Unknown)
        {
            return;
        }
        
        if (eventType is UserVoiceChannelActionType.StreamStarted)
        {
            await _streamStartedRequestChannel.Writer.WriteAsync(new StreamStartedRequest
            {
                GuildId = voiceStateAfter.VoiceChannel.Guild.Id, ChannelId = voiceStateAfter.VoiceChannel.Id,
                Timestamp = DateTimeOffset.Now, UserId = user.Id
            });
            return;
        }

        var eventMessage = new UserVoiceChannelActionEvent
        {
            GuildId = voiceStateAfter.VoiceChannel.Guild.Id,
            ChannelId = voiceStateAfter.VoiceChannel.Id,
            UserId = user.Id,
            Timestamp = DateTimeOffset.Now,
            ActionType = eventType
        };
        await ProduceEventAsync(_serviceProvider, eventMessage);
    }

    private async Task OnClientOnMessageDeleted(
        Cacheable<IMessage, ulong> cacheableMessage,
        Cacheable<IMessageChannel, ulong> cacheableMessageChannel)
    {
        if (!cacheableMessage.HasValue) return;

        var eventMessage = new MessageDeletedEvent
            { Timestamp = cacheableMessage.Value.Timestamp, MessageId = cacheableMessage.Value.Id, };

        await ProduceEventAsync(_serviceProvider, eventMessage);
    }

    private async Task OnClientOnMessageReceived(SocketMessage message)
    {
        if (message.Channel is not SocketGuildChannel guildChannel) return;

        var eventMessage = new MessageReceivedEvent
        {
            GuildId = guildChannel.Guild.Id,
            ChannelId = guildChannel.Id,
            UserId = message.Author.Id,
            MessageId = message.Id,
            Content = message.Content,
            Timestamp = message.CreatedAt
        };

        await ProduceEventAsync(_serviceProvider, eventMessage);
    }

    private async Task OnClientOnMessageUpdated(
        Cacheable<IMessage, ulong> cacheableMessage,
        SocketMessage messageBefore,
        ISocketMessageChannel cacheableMessageChannel)
    {
        if (cacheableMessage.Value.Content is "" || messageBefore.Content is "")
            return;

        if (cacheableMessage.Value.Content == messageBefore.Content)
            return;

        var eventMessage = new MessageUpdatedEvent
        {
            MessageId = cacheableMessage.Value.Id, NewContent = cacheableMessage.Value.Content,
            Timestamp = DateTimeOffset.Now
        };

        await ProduceEventAsync(_serviceProvider, eventMessage);
    }

    private async Task ProduceEventAsync<T>(IServiceProvider serviceProvider, T eventMessage) where T : class
    {
        if (!_options.SendEvents)
            return;

        using var scope = serviceProvider.CreateScope();
        var topicProducer = scope.ServiceProvider.GetRequiredService<ITopicProducer<Guid, T>>();
        await topicProducer.Produce(Guid.NewGuid(), eventMessage);
    }

    // private async Task PollingStreamPreviewAsync()
    // {
    //     if (_requestClient.Rest is not DiscordRestClient restClient)
    //     {
    //         _logger.LogCritical($"{nameof(PollingStreamPreviewAsync)} is stopped because" +
    //                             $" {nameof(DiscordRestClient)} cannot cast to DiscordRestClient");
    //         return;
    //     }
    //
    //     await foreach (var request in _streamStartedRequestChannel.Reader.ReadAllAsync())
    //     {
    //         var channel = await _requestClient.GetChannelAsync(request.ChannelId);
    //         var user = await channel.GetUserAsync(request.UserId);
    //         if (user is SocketGuildUser { IsStreaming: false })
    //             continue;
    //
    //         string? streamPreview = null;
    //         try
    //         {
    //             streamPreview = await restClient.GetUserStreamPreviewAsync(
    //                 request.GuildId,
    //                 request.ChannelId,
    //                 request.UserId);
    //         }
    //         catch (HttpException)
    //         {
    //         }
    //
    //         if (string.IsNullOrEmpty(streamPreview))
    //         {
    //             await _streamStartedRequestChannel.Writer.WriteAsync(request);
    //             await Task.Delay(7000);
    //             continue;
    //         }
    //
    //         var eventMessage = new UserVoiceChannelActionEvent
    //         {
    //             GuildId = request.GuildId,
    //             ChannelId = request.ChannelId,
    //             UserId = request.UserId,
    //             Timestamp = request.Timestamp,
    //             Attachment = streamPreview,
    //             ActionType = UserVoiceChannelActionType.StreamStarted
    //         };
    //         await ProduceEventAsync(_serviceProvider, eventMessage);
    //
    //         await Task.Delay(7000);
    //     }
    // }
    
    public void Dispose()
    {
        _client?.Dispose();
    }
}