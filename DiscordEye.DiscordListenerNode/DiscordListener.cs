using System.Collections.Concurrent;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordEye.DiscordListenerNode.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordEye.DiscordListenerNode;

public class DiscordListener
{
    private readonly DiscordSocketClient _client;
    private readonly StartupOptions _options;
    private readonly ILogger<DiscordListener> _logger;
    private readonly ITopicProducer<Guid, MessageDeletedEvent> _messageDeletedEvent;
    private readonly ITopicProducer<Guid, StreamStartedEvent> _streamStartedEvent;
    private readonly ITopicProducer<Guid, StreamStoppedEvent> _streamStoppedEvent;
    private readonly ConcurrentQueue<StreamStartedRequest> _streamStartedRequest;

    public DiscordListener(
        IOptions<StartupOptions> options,
        ILogger<DiscordListener> logger,
        ITopicProducer<Guid, MessageDeletedEvent> messageDeletedEvent,
        ITopicProducer<Guid, StreamStartedEvent> streamStartedEvent,
        ITopicProducer<Guid, StreamStoppedEvent> streamStoppedEvent)
    {
        _streamStartedRequest = new ConcurrentQueue<StreamStartedRequest>();
        _logger = logger;
        _messageDeletedEvent = messageDeletedEvent;
        _streamStartedEvent = streamStartedEvent;
        _streamStoppedEvent = streamStoppedEvent;
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
                // user changed avatar
            }
        };
        
        _client.GuildMemberUpdated += async (cacheable, user) =>
        {
            var before = await cacheable.GetOrDownloadAsync();

            if (before.Username != user.Username)
            {
                // user changed nickname
            }
        };
        
        _client.UserBanned += async (user, guild) =>
        {
            // user banned
        };
        
        _client.UserVoiceStateUpdated += async (user, voiceStateBefore, voiceStateAfter) =>
        {
            if (voiceStateBefore.VoiceChannel == null && voiceStateAfter.VoiceChannel != null)
            {
                _logger.LogInformation($"{user.Username} joined {voiceStateAfter.VoiceChannel.Name}");
                // user joined to channel
            }
            if (voiceStateBefore.VoiceChannel != null && voiceStateAfter.VoiceChannel == null)
            {
                _logger.LogInformation($"{user.Username} left {voiceStateBefore.VoiceChannel.Name}");
                // user left from channel                
            }
            
            if (voiceStateBefore.IsSelfDeafened && !voiceStateAfter.IsSelfDeafened)
            {
                _logger.LogInformation($"{user.Username} turned on headphones in {voiceStateBefore.VoiceChannel.Name}");
                // turn on headphones
            }
            if (!voiceStateBefore.IsSelfDeafened && voiceStateAfter.IsSelfDeafened)
            {
                _logger.LogInformation($"{user.Username} turned off headphones in {voiceStateBefore.VoiceChannel.Name}");
                // turn off headphones
            }
            
            if (voiceStateBefore.IsSelfMuted && !voiceStateAfter.IsSelfMuted)
            {
                _logger.LogInformation($"{user.Username} turned on mic in {voiceStateBefore.VoiceChannel.Name}");
                // turn on mic
            }
            if (!voiceStateBefore.IsSelfMuted && voiceStateAfter.IsSelfMuted)
            {
                _logger.LogInformation($"{user.Username} turned off mic in {voiceStateBefore.VoiceChannel.Name}");
                // turn off mic
            }
            
            if (!voiceStateBefore.IsVideoing && voiceStateAfter.IsVideoing)
            {
                _logger.LogInformation($"{user.Username} turned on webcamera in {voiceStateBefore.VoiceChannel.Name}");
                // turn on webcamera
            }
            if (voiceStateBefore.IsVideoing && !voiceStateAfter.IsVideoing)
            {
                _logger.LogInformation($"{user.Username} turned off webcamera in {voiceStateBefore.VoiceChannel.Name}");
                // turn off webcamera
            }
            
            if (voiceStateBefore.IsStreaming && !voiceStateAfter.IsStreaming)
            {
                var eventMessage = new StreamEvent
                {
                    GuildId = voiceStateBefore.VoiceChannel.Guild.Id,
                    ChannelId = voiceStateBefore.VoiceChannel.Id,
                    UserId = user.Id,
                    Timestamp = DateTimeOffset.Now
                };
                _logger.LogInformation(eventMessage.ToString());
                
                await _streamStoppedEvent.Produce(
                    Guid.NewGuid(),
                    eventMessage);
            }
            if (!voiceStateBefore.IsStreaming && voiceStateAfter.IsStreaming)
            {
                _streamStartedRequest.Enqueue(
                    new StreamStartedRequest
                    {
                        GuildId = voiceStateAfter.VoiceChannel.Guild.Id,
                        ChannelId = voiceStateAfter.VoiceChannel.Id,
                        Timestamp = DateTimeOffset.Now,
                        UserId = user.Id
                    });
            }
        };

        _client.MessageDeleted += async (cacheableMessage, cacheableMessageChannel) =>
        {
            if (!cacheableMessage.HasValue) return;
            if (cacheableMessageChannel.Value is not SocketTextChannel channel) return;
            
            var eventMessage = new MessageDeletedEvent
            {
                GuildId = channel.Guild.Id,
                ChannelId = channel.Id,
                UserId = cacheableMessage.Value.Author.Id,
                MessageId = cacheableMessage.Value.Id,
                Content = cacheableMessage.Value.Content,
                Timestamp = cacheableMessage.Value.Timestamp
            };
            _logger.LogInformation(eventMessage.ToString());

            if (_options.SendEvent)
                await _messageDeletedEvent.Produce(Guid.NewGuid(), eventMessage);
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
        
        while (true)
        {
            if (!_streamStartedRequest.TryDequeue(out var requestInfo))
            {
                await Task.Delay(7000);
                continue;
            }

            var channel = await _client.GetChannelAsync(requestInfo.ChannelId);
            var user = await channel.GetUserAsync(requestInfo.UserId);
            if (user is SocketGuildUser { IsStreaming: false })
                continue;

            var streamPreview = string.Empty;
            try
            {
                streamPreview = await restClient.GetUserStreamPreviewAsync(
                    requestInfo.GuildId,
                    requestInfo.ChannelId,
                    requestInfo.UserId);
            }
            catch (Discord.Net.HttpException e)
            {
                await Task.Delay(7000);
            }


            if (string.IsNullOrEmpty(streamPreview))
            {
                _streamStartedRequest.Enqueue(requestInfo);
                await Task.Delay(7000);
                continue;
            }

            var eventMessage = new StreamEvent()
            {
                GuildId = requestInfo.GuildId,
                ChannelId = requestInfo.ChannelId,
                UserId = requestInfo.UserId,
                Url = streamPreview,
                Timestamp = requestInfo.Timestamp
            };
            _logger.LogInformation(eventMessage.ToString());

            if (_options.SendEvent)
                await _streamStartedEvent.Produce(Guid.NewGuid(), eventMessage);
            
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