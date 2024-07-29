using System.Collections.Concurrent;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordEye.DiscordListenerNode;

public class DiscordListener
{
    private readonly DiscordSocketClient _client;
    private readonly StartupOptions _options;
    private readonly ILogger<DiscordListener> _logger;
    private readonly ITopicProducer<Guid, DiscordMessageDeleteEvent> _messageDeleteTopicProducer;
    private readonly ConcurrentQueue<StreamPreviewRequestInfo> _streamPreviewRequestInfos;

    public DiscordListener(
        IOptions<StartupOptions> options,
        ILogger<DiscordListener> logger,
        ITopicProducer<Guid, DiscordMessageDeleteEvent> messageDeleteTopicProducer)
    {
        _streamPreviewRequestInfos = new ConcurrentQueue<StreamPreviewRequestInfo>();
        _logger = logger;
        _messageDeleteTopicProducer = messageDeleteTopicProducer;
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
        
        _client.UserVoiceStateUpdated += (user, voiceStateBefore, voiceStateAfter) =>
        {
            if (voiceStateBefore.VoiceChannel == null && voiceStateAfter.VoiceChannel != null)
            {
                // user joined to channel
            }
            if (voiceStateBefore.VoiceChannel != null && voiceStateAfter.VoiceChannel == null)
            {
                // user left from channel                
            }
            
            if (voiceStateBefore.IsSelfDeafened && !voiceStateAfter.IsSelfDeafened)
            {
                // turn on headphones
            }
            if (!voiceStateBefore.IsSelfDeafened && voiceStateAfter.IsSelfDeafened)
            {
                // turn off headphones
            }
            
            if (voiceStateBefore.IsSelfMuted && !voiceStateAfter.IsSelfMuted)
            {
                // turn on mic
            }
            if (!voiceStateBefore.IsSelfMuted && voiceStateAfter.IsSelfMuted)
            {
                // turn off mic
            }
            
            if (!voiceStateBefore.IsVideoing && voiceStateAfter.IsVideoing)
            {
                // turn on webcamera
            }
            if (voiceStateBefore.IsVideoing && !voiceStateAfter.IsVideoing)
            {
                // turn off webcamera
            }
            
            if (voiceStateBefore.IsStreaming && !voiceStateAfter.IsStreaming)
            {
                // turn off stream
            }
            if (!voiceStateBefore.IsStreaming && voiceStateAfter.IsStreaming)
            {
                _streamPreviewRequestInfos.Enqueue(
                    new StreamPreviewRequestInfo
                    {
                        GuildId = voiceStateAfter.VoiceChannel.Guild.Id,
                        ChannelId = voiceStateAfter.VoiceChannel.Id,
                        UserId = user.Id
                    });
            }

            return Task.CompletedTask;
        };

        _client.MessageDeleted += async (cacheableMessage, cacheableMessageChannel) =>
        {
            if (!cacheableMessage.HasValue) return;
            if (cacheableMessageChannel.Value is not SocketTextChannel channel) return;
            
            var eventMessage = new DiscordMessageDeleteEvent
            {
                GuildId = channel.Guild.Id,
                ChannelId = channel.Id,
                UserId = cacheableMessage.Value.Author.Id,
                MessageId = cacheableMessage.Value.Id,
                Content = cacheableMessage.Value.Content,
                DeletedAt = cacheableMessage.Value.Timestamp
            };
            
            _logger.LogInformation(eventMessage.ToString());
            await _messageDeleteTopicProducer.Produce(Guid.NewGuid(), eventMessage);
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
            if (!_streamPreviewRequestInfos.TryDequeue(out var requestInfo))
            {
                await Task.Delay(5000);
                continue;
            }

            var channel = await _client.GetChannelAsync(requestInfo.ChannelId);
            var user = await channel.GetUserAsync(requestInfo.UserId);
            if (user is SocketGuildUser { IsStreaming: false })
            {
                _logger.LogInformation($"{requestInfo.UserId} stream is ending. deleted from queue");
                continue;
            }

            var streamPreview = await restClient.GetUserStreamPreviewAsync(
                requestInfo.GuildId,
                requestInfo.ChannelId,
                requestInfo.UserId);

            if (string.IsNullOrEmpty(streamPreview))
            {
                _streamPreviewRequestInfos.Enqueue(requestInfo);
                await Task.Delay(7000);
                continue;
            }

            _logger.LogInformation($"{requestInfo.UserId} stream preview {streamPreview}");
            await Task.Delay(7000);
        }
    }
    
    public async Task StartAsync()
    {
        await _client.LoginAsync(TokenType.User, _options.Token);
        await _client.StartAsync();
        _logger.LogInformation($"SelfBot started with token {_options.Token[..10]} in {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

        await PollingStreamPreviewAsync();
    }
}