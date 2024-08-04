using System.Text.Json;
using System.Threading.Channels;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordEye.DiscordListenerNode;

public class DiscordListener
{
    private readonly DiscordSocketClient _client;
    private readonly StartupOptions _options;
    private readonly ILogger<DiscordListener> _logger;
    private readonly ITopicProducer<Guid, DiscordEvent> _discordTopic;
    private readonly Channel<StreamStartedRequest> _streamStartedRequestChannel;

    public DiscordListener(
        IOptions<StartupOptions> options,
        ILogger<DiscordListener> logger,
        ITopicProducer<Guid, DiscordEvent> discordTopic)
    {
        _streamStartedRequestChannel = Channel.CreateUnbounded<StreamStartedRequest>();
        _logger = logger;
        _discordTopic = discordTopic;
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
                _logger.LogInformation($"{_client.CurrentUser.Id}:{_client.CurrentUser.Username}" +
                                       $" subscribed to {guild.Name}");
            }
        };

        _client.UserUpdated += async (userBefore, userAfter) =>
        {
            if (userBefore.GetAvatarUrl() != userAfter.GetAvatarUrl())
            {
                var eventMessage = new DiscordEvent
                {
                    EventType = DiscordEventType.UserChangedAvatar,
                    ContentJson = JsonSerializer.Serialize(new UserChangedAvatarEvent
                    {
                        UserId = userBefore.Id,
                        OldAvatarUrl = userBefore.GetAvatarUrl(),
                        NewAvatarUrl = userAfter.GetAvatarUrl(),
                        Timestamp = DateTimeOffset.Now
                    })
                };

                await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
            }
        };
        
        _client.GuildMemberUpdated += async (cacheable, user) =>
        {
            var before = await cacheable.GetOrDownloadAsync();

            if (before.Username != user.Username)
            {
                var eventMessage = new DiscordEvent
                {
                    EventType = DiscordEventType.UserGuildChangedNickname,
                    ContentJson = JsonSerializer.Serialize(new UserGuildChangedNicknameEvent
                    {
                        GuildId = before.Guild.Id,
                        UserId = user.Id,
                        OldUsername = before.Username,
                        NewUsername = user.Username,
                        Timestamp = DateTimeOffset.Now
                    })
                };

                await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
            }
        };
        
        _client.UserBanned += async (user, guild) =>
        {
            var eventMessage = new DiscordEvent
            {
                EventType = DiscordEventType.Banned,
                ContentJson = JsonSerializer.Serialize(new UserBannedEvent
                {
                    GuildId = guild.Id,
                    UserId = user.Id,
                    Timestamp = DateTimeOffset.Now
                })
            };

            await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
        };
        
        _client.UserVoiceStateUpdated += async (user, voiceStateBefore, voiceStateAfter) =>
        {
            var eventType = DiscordHelper.DetermineEventType(voiceStateBefore, voiceStateAfter);

            if (eventType is DiscordEventType.StreamStarted)
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

            var eventMessage = new DiscordEvent
            {
                EventType = eventType,
                ContentJson = JsonSerializer.Serialize(new UserVoiceChannelActionEvent
                {
                    GuildId = voiceStateAfter.VoiceChannel.Guild.Id,
                    ChannelId = voiceStateAfter.VoiceChannel.Id,
                    UserId = user.Id,
                    Timestamp = DateTimeOffset.Now
                })
            };
            await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
        };

        _client.MessageDeleted += async (cacheableMessage, cacheableMessageChannel) =>
        {
            if (!cacheableMessage.HasValue) return;
            if (cacheableMessageChannel.Value is not SocketTextChannel channel) return;

            var eventMessage = new DiscordEvent
            {
                EventType = DiscordEventType.MessageDeleted,
                ContentJson = JsonSerializer.Serialize(new MessageDeletedEvent
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    UserId = cacheableMessage.Value.Author.Id,
                    Content = cacheableMessage.Value.Content,
                    Timestamp = cacheableMessage.Value.Timestamp,
                    MessageId = cacheableMessage.Value.Id,
                })
            };

            await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
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

            var eventMessage = new DiscordEvent
            {
                EventType = DiscordEventType.MessageChanged,
                ContentJson = JsonSerializer.Serialize(new MessageUpdatedEvent
                {
                    GuildId = channel.Guild.Id,
                    ChannelId = channel.Id,
                    UserId = cacheableMessage.Value.Author.Id,
                    MessageId = cacheableMessage.Value.Id,
                    OldContent = messageBefore.Content,
                    NewContent = cacheableMessage.Value.Content,
                    Timestamp = DateTimeOffset.Now
                })
            };

            await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
        };
    }
    
    private async Task PollingStreamPreviewAsync()
    {
        if (_client.Rest is not DiscordRestClient restClient)
        {
            _logger.LogCritical($"{nameof(PollingStreamPreviewAsync)} is stopped because" +
                                $" {nameof(DiscordRestClient)} cannot cast to DiscordRestClient");
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

            var eventMessage = new DiscordEvent
            {
                EventType = DiscordEventType.StreamStarted,
                ContentJson = JsonSerializer.Serialize(new UserVoiceChannelActionEvent
                {
                    GuildId = request.GuildId,
                    ChannelId = request.ChannelId,
                    UserId = request.UserId,
                    Timestamp = request.Timestamp,
                    Attachment = streamPreview
                })
            };
            await _discordTopic.Produce(Guid.NewGuid(), eventMessage);
            
            await Task.Delay(7000);
        }
    }
    
    public async Task StartAsync()
    {
        await _client.LoginAsync(TokenType.User, _options.Token);
        await _client.StartAsync();
        _logger.LogInformation($"SelfBot started with token {_options.Token[..10]}" +
                               $" in {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

        await PollingStreamPreviewAsync();
    }
}