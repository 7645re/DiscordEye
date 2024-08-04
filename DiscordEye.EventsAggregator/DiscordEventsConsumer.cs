using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator;

public class DiscordEventsConsumer : IConsumer<DiscordEvent>
{
    private readonly ILogger<DiscordEventsConsumer> _logger;

    public DiscordEventsConsumer(ILogger<DiscordEventsConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<DiscordEvent> context)
    {
        _logger.LogInformation(context.Message.ContentJson);
        return Task.CompletedTask;
    }
}