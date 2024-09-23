using System.Threading.Channels;
using Discord;
using Discord.WebSocket;
using DiscordEye.Node.Data;
using DiscordEye.Node.Helpers;
using DiscordEye.Node.Options;
using DiscordEye.Shared.Events;
using DiscordEye.Shared.Extensions;
using MassTransit;
using Microsoft.Extensions.Options;

namespace DiscordEye.Node.DiscordClientWrappers.EventClient;

public class DiscordEventClient : IDiscordEventClient
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordOptions _options;
    private readonly ILogger<DiscordEventClient> _logger;
    private readonly Channel<StreamStartedRequest> _streamStartedRequestChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _token;

    public DiscordEventClient(
        IOptions<DiscordOptions> discordOptions,
        IServiceProvider serviceProvider,
        ILogger<DiscordEventClient> logger)
    {
        _streamStartedRequestChannel = Channel.CreateUnbounded<StreamStartedRequest>();
        _options = discordOptions.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _token = StartupExtensions.GetDiscordTokenFromEnvironment();
        _client = InitClientAsync().GetAwaiter().GetResult();
    }

    public async Task<DiscordSocketClient> InitClientAsync()
    {
        var discordSocketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All
        };

        var client = new DiscordSocketClient(discordSocketConfig);
        RegisterEventsHandlers();
        await client.LoginAsync(TokenType.User, _token);
        await client.StartAsync();

        return client;
        void RegisterEventsHandlers()
        {
            client.Ready += OnClientOnReady;
            client.UserUpdated += OnClientOnUserUpdated;
            client.GuildMemberUpdated += OnClientOnGuildMemberUpdated;
            client.UserBanned += OnClientOnUserBanned;
            client.UserVoiceStateUpdated += OnClientOnUserVoiceStateUpdated;
            client.MessageDeleted += OnClientOnMessageDeleted;
            client.MessageReceived += OnClientOnMessageReceived;
            client.MessageUpdated += OnClientOnMessageUpdated;
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
}