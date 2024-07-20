using Discord;
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

    public DiscordListener(
        IOptions<StartupOptions> options,
        ILogger<DiscordListener> logger,
        ITopicProducer<Guid, DiscordMessageDeleteEvent> messageDeleteTopicProducer)
    {
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
            foreach (var guildId in _client.Guilds.Select(x => x.Id))
            {
                await _client.SubscribeToGuildEvents(guildId);
                _logger.LogInformation($"{_client.CurrentUser.Id} subscribed to guild {guildId}");
            }
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

    public async Task StartAsync()
    {
        _logger.LogInformation($"Starting up with token {_options.Token[..10]} " +
                               $"in {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

        await _client.LoginAsync(TokenType.User, _options.Token);
        _logger.LogInformation($"Logged in with token {_options.Token[..10]}");

        await _client.StartAsync();
        _logger.LogInformation($"SelfBot started with token {_options.Token[..10]}");
    }
}