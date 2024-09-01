using System.Net;
using System.Threading.Channels;
using Discord;
using Discord.Net;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.DiscordListener;
using DiscordEye.Node.Dto;
using DiscordEye.Node.Mappers;
using DiscordEye.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DiscordEye.Node;

public class DiscordListenerBackgroundService : BackgroundService
{
    private readonly StartupOptions _options;
    private readonly ILogger<DiscordListenerBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<StreamStartedRequest> _streamStartedRequestChannel;
    private CancellationToken? serviceCancellationToken;
    private DiscordSocketClient _requestClient;
    private readonly DiscordSocketClient _listenerClient;

    public DiscordListenerBackgroundService(
        IOptions<StartupOptions> options,
        ILogger<DiscordListenerBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _streamStartedRequestChannel = Channel.CreateUnbounded<StreamStartedRequest>();
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _listenerClient = InitializeListenerClient();
        _requestClient = InitializeRequestClient();
    }

    private DiscordSocketClient InitializeListenerClient()
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            MessageCacheSize = _options.MessageCacheSize
        };

        return RegisterEventsHandlers(new DiscordSocketClient(discordSocketConfig));

        DiscordSocketClient RegisterEventsHandlers(DiscordSocketClient client)
        {
            client.Ready += OnClientOnReady;
            client.UserUpdated += OnClientOnUserUpdated;
            client.GuildMemberUpdated += OnClientOnGuildMemberUpdated;
            client.UserBanned += OnClientOnUserBanned;
            client.UserVoiceStateUpdated += OnClientOnUserVoiceStateUpdated;
            client.MessageDeleted += OnClientOnMessageDeleted;
            client.MessageReceived += OnClientOnMessageReceived;
            client.MessageUpdated += OnClientOnMessageUpdated;
            return client;
        }
    }

    private DiscordSocketClient InitializeRequestClient(WebProxy? webProxy = null)
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        if (webProxy is not null)
        {
            discordSocketConfig.RestClientProvider = DefaultRestClientProvider.Create(webProxy: webProxy);
        }

        return new DiscordSocketClient(discordSocketConfig);
    }
    
    private async Task OnClientOnReady()
    {
        foreach (var guild in _listenerClient.Guilds)
        {
            await _listenerClient.SubscribeToGuildEvents(guild.Id);
            _logger.LogInformation(
                $"{_listenerClient.CurrentUser.Id}:{_listenerClient.CurrentUser.Username} subscribed to {guild.Name}");
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

    private async Task OnClientOnMessageDeleted(Cacheable<IMessage, ulong> cacheableMessage,
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

    private async Task OnClientOnMessageUpdated(Cacheable<IMessage, ulong> cacheableMessage,
        SocketMessage messageBefore, ISocketMessageChannel cacheableMessageChannel)
    {
        if (cacheableMessage.Value.Content is "" || messageBefore.Content is "")
        {
            return;
        }

        if (cacheableMessage.Value.Content == messageBefore.Content)
        {
            return;
        }

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

    private async Task PollingStreamPreviewAsync()
    {
        if (_requestClient.Rest is not DiscordRestClient restClient)
        {
            _logger.LogCritical($"{nameof(PollingStreamPreviewAsync)} is stopped because" +
                                $" {nameof(DiscordRestClient)} cannot cast to DiscordRestClient");
            return;
        }

        await foreach (var request in _streamStartedRequestChannel.Reader.ReadAllAsync())
        {
            var channel = await _requestClient.GetChannelAsync(request.ChannelId);
            var user = await channel.GetUserAsync(request.UserId);
            if (user is SocketGuildUser { IsStreaming: false })
                continue;

            string? streamPreview = null;
            try
            {
                streamPreview = await restClient.GetUserStreamPreviewAsync(
                    request.GuildId,
                    request.ChannelId,
                    request.UserId);
            }
            catch (HttpException)
            {
            }

            if (string.IsNullOrEmpty(streamPreview))
            {
                await _streamStartedRequestChannel.Writer.WriteAsync(request);
                await Task.Delay(7000);
                continue;
            }

            var eventMessage = new UserVoiceChannelActionEvent
            {
                GuildId = request.GuildId,
                ChannelId = request.ChannelId,
                UserId = request.UserId,
                Timestamp = request.Timestamp,
                Attachment = streamPreview,
                ActionType = UserVoiceChannelActionType.StreamStarted
            };
            await ProduceEventAsync(_serviceProvider, eventMessage);

            await Task.Delay(7000);
        }
    }

    public async Task<DiscordUser> GetUserAsync(
        ulong id,
        bool withGuilds = false)
    {
        try
        {
            var userProfile = await _requestClient.Rest.GetUserProfileAsync(id);
            var userRestGuilds = new List<DiscordGuild>();
            if (withGuilds)
            {
                userRestGuilds.AddRange(
                    userProfile
                        .MutualGuilds
                        .Select(mutualGuild =>
                            GetGuild(mutualGuild.Id)));
            }

            var user = userProfile.ToDiscordUser(userRestGuilds);
            return user;
        }
        catch (CloudFlareException e)
        {
            _requestClient = InitializeRequestClient(new WebProxy(new Uri("http://147.45.229.223:3128"))
            {
                Credentials = new NetworkCredential
                {
                    UserName = "aMgZms9_LGpvmmf0qGsm",
                    Password = "m~XzPxW80b2g4bbaZnxj,MXe9v_"
                }
            });

            if (serviceCancellationToken is not null)
                await ExecuteAsync(serviceCancellationToken.Value);

            throw;
        }
    }

    public DiscordGuild? GetGuild(
        ulong id,
        bool withChannels = false)
    {
        var guild = _requestClient.GetGuild(id);
        return guild.ToDiscordGuild(withChannels);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        serviceCancellationToken = stoppingToken;

        await _requestClient.LoginAsync(TokenType.User, _options.Token);
        await _requestClient.StartAsync();

        await _listenerClient.LoginAsync(TokenType.User, _options.Token);
        await _listenerClient.StartAsync();
        
        _logger.LogInformation($"SelfBot started with token {_options.Token[..10]}" +
                               $" in {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
        // await PollingStreamPreviewAsync();
    }
}