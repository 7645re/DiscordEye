using System.Threading.Channels;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.DiscordListener.Dto;
using DiscordEye.DiscordListener.Mappers;
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

        // TODO: ссылка на старую аватарку уже недействительная 
        _client.UserUpdated += async (userBefore, userAfter) =>
        {
            if (userBefore.GetAvatarUrl() != userAfter.GetAvatarUrl())
            {
                var eventMessage = new UserChangedAvatarEvent
                {
                    UserId = userBefore.Id,
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
                    GuildId = before.Guild.Id,
                    UserId = user.Id,
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
                GuildId = guild.Id,
                UserId = user.Id,
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
                GuildId = voiceStateAfter.VoiceChannel.Guild.Id,
                ChannelId = voiceStateAfter.VoiceChannel.Id,
                UserId = user.Id,
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
                MessageId = cacheableMessage.Value.Id,
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        };

        _client.MessageReceived += async message =>
        {
            if (message.Channel is not SocketGuildChannel guildChannel)
                return;

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
        };
        
        _client.MessageUpdated += async (cacheableMessage, messageBefore, cacheableMessageChannel) =>
        {
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
                MessageId = cacheableMessage.Value.Id,
                NewContent = cacheableMessage.Value.Content,
                Timestamp = DateTimeOffset.Now
            };

            await ProduceEventAsync(_serviceProvider, eventMessage);
        };
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
        var userProfile = await _client.Rest.GetUserProfileAsync(id);
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

    public DiscordGuild GetGuild(
        ulong id,
        bool withChannels = false)
    {
        var guild = _client.GetGuild(id); 
        return guild.ToDiscordGuild(withChannels);
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
