using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator.Consumers;

public class MessageUpdatedEventConsumer : IConsumer<MessageUpdatedEvent>
{
    private readonly ILogger<MessageUpdatedEventConsumer> _logger;
    private readonly ApplicationDbContext _applicationDbContext;

    public MessageUpdatedEventConsumer(
        ILogger<MessageUpdatedEventConsumer> logger,
        ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
    }

    public async Task Consume(ConsumeContext<MessageUpdatedEvent> context)
    {
        
    }
}