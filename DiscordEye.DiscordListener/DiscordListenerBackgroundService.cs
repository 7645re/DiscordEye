using System.Threading.Channels;
using Discord;
using Discord.API;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DiscordEye.DiscordListener;

public class DiscordListenerBackgroundService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly StartupOptions _options;
    private readonly ILogger<DiscordListenerBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Channel<StreamStartedRequest> _streamStartedRequestChannel;

    public DiscordListenerBackgroundService(
        IOptions<StartupOptions> options,
        ILogger<DiscordListenerBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _streamStartedRequestChannel = Channel.CreateUnbounded<StreamStartedRequest>();
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            MessageCacheSize = _options.MessageCacheSize
        });

        _client.Ready += async () =>
        {
            foreach (var guild in _client.Guilds)
            {
                await _client.SubscribeToGuildEvents(guild.Id);
                _logger.LogInformation($"{_client.CurrentUser.Id}:{_client.CurrentUser.Username} subscribed to {guild.Name}");
            }
        };

        _client.UserUpdated += async (userBefore, userAfter) =>
        {
            if (userBefore.GetAvatarUrl() != userAfter.GetAvatarUrl())
            {
                var eventMessage = new UserChangedAvatarEvent
                {
                    UserId = (long)userBefore.Id,
                    OldAvatarUrl = userBefore.GetAvatarUrl(),
                    NewAvatarUrl = userAfter.GetAvatarUrl(),
                    Timestamp = DateTimeOffset.Now
                };

                await ProduceEventAsync(_serviceProvider, eventMessage);
            }
        };
        
        _client.GuildMemberUpdated += async (cacheable, user) =>
        {
            var before = await cacheable.GetOrDownloadAsync();

            if (before.Nickname != user.Nickname)
            {
                var eventMessage = new UserGuildChangedNicknameEvent
                {
                    GuildId = (long)before.Guild.Id,
                    UserId = (long)user.Id,
                    OldUsername = before.Nickname,
                    NewUsername = user.Nickname,
                    Timestamp = DateTimeOffset.Now
                };

                await ProduceEventAsync(_serviceProvider, eventMessage);
            }
        };
        
        _client.UserBanned += async (user, guild) =>
        {
            var eventMessage = new UserBannedEvent
            {
                GuildId = (long)guild.Id,
                UserId = (long)user.Id,
                Timestamp = DateTimeOffset.Now
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        };
        
        _client.UserVoiceStateUpdated += async (user, voiceStateBefore, voiceStateAfter) =>
        {
            var eventType = DiscordHelper.DetermineEventType(voiceStateBefore, voiceStateAfter);

            if (eventType is UserVoiceChannelActionType.StreamStarted)
            {
                await _streamStartedRequestChannel.Writer.WriteAsync(
                    new StreamStartedRequest
                    {
                        GuildId = voiceStateAfter.VoiceChannel.Guild.Id,
                        ChannelId = voiceStateAfter.VoiceChannel.Id,
                        Timestamp = DateTimeOffset.Now,
                        UserId = user.Id
                    });
                return;
            }

            var eventMessage = new UserVoiceChannelActionEvent
            {
                GuildId = (long)voiceStateAfter.VoiceChannel.Guild.Id,
                ChannelId = (long)voiceStateAfter.VoiceChannel.Id,
                UserId = (long)user.Id,
                Timestamp = DateTimeOffset.Now,
                ActionType = eventType
            };
            await ProduceEventAsync(_serviceProvider, eventMessage);
        };

        _client.MessageDeleted += async (cacheableMessage, cacheableMessageChannel) =>
        {
            if (!cacheableMessage.HasValue) return;

            var eventMessage = new MessageDeletedEvent
            {
                Timestamp = cacheableMessage.Value.Timestamp,
                MessageId = (long)cacheableMessage.Value.Id,
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        };

        _client.MessageReceived += async message =>
        {
            if (message.Channel is not SocketGuildChannel guildChannel)
                return;

            var eventMessage = new MessageReceivedEvent
            {
                GuildId = (long)guildChannel.Guild.Id,
                ChannelId = (long)guildChannel.Id,
                UserId = (long)message.Author.Id,
                MessageId = (long)message.Id,
                Content = message.Content,
                Timestamp = message.CreatedAt
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        };
        
        _client.MessageUpdated += async (cacheableMessage, messageBefore, cacheableMessageChannel) =>
        {
            if (cacheableMessageChannel is not SocketTextChannel channel) return;
            if (cacheableMessage.Value.Content is ""
                || messageBefore.Content is "")
            {
                return;
            }

            if (cacheableMessage.Value.Content == messageBefore.Content)
            {
                return;
            }

            var eventMessage = new MessageUpdatedEvent
            {
                MessageId = (long)cacheableMessage.Value.Id,
                NewContent = cacheableMessage.Value.Content,
                Timestamp = DateTimeOffset.Now
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        };
    }

    private async Task ProduceEventAsync<T>(IServiceProvider serviceProvider, T eventMessage) where T : class
    {
        using var scope = serviceProvider.CreateScope();
        var topicProducer = scope.ServiceProvider.GetRequiredService<ITopicProducer<Guid, T>>();
        await topicProducer.Produce(Guid.NewGuid(), eventMessage);
    }
    
    private async Task PollingStreamPreviewAsync()
    {
        if (_client.Rest is not DiscordRestClient restClient)
        {
            _logger.LogCritical($"{nameof(PollingStreamPreviewAsync)} is stopped because {nameof(DiscordRestClient)} cannot cast to DiscordRestClient");
            return;
        }

        await foreach (var request in _streamStartedRequestChannel.Reader.ReadAllAsync())
        {
            var channel = await _client.GetChannelAsync(request.ChannelId);
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
            catch (Discord.Net.HttpException)
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
                GuildId = (long)request.GuildId,
                ChannelId = (long)request.ChannelId,
                UserId = (long)request.UserId,
                Timestamp = request.Timestamp,
                Attachment = streamPreview,
                ActionType = UserVoiceChannelActionType.StreamStarted
            };
            await ProduceEventAsync(_serviceProvider, eventMessage);

            await Task.Delay(7000);
        }
    }

    public async Task<IChannel> GetChannelAsync(ulong id)
    {
        return await _client.Rest.GetChannelAsync(id);
    }
    
    public async Task<UserProfile> GetUserProfileAsync(ulong id)
    {
        return await _client.Rest.GetUserProfileAsync(id);
    }

    public async Task<RestGuild> GetGuildAsync(ulong id)
    {
        return await _client.Rest.GetGuildAsync(id);
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.LoginAsync(TokenType.User, _options.Token);
        await _client.StartAsync();
        _logger.LogInformation($"SelfBot started with token {_options.Token[..10]}" +
                               $" in {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

        await PollingStreamPreviewAsync();
    }
}
