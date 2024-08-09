using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator.Consumers;

public class MessageDeletedEventConsumer : IConsumer<MessageDeletedEvent>
{
    private readonly ILogger<MessageDeletedEventConsumer> _logger;
    private readonly ApplicationDbContext _applicationDbContext;

    public MessageDeletedEventConsumer(
        ILogger<MessageDeletedEventConsumer> logger,
        ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
    }

    public async Task Consume(ConsumeContext<MessageDeletedEvent> context)
    {
        
    }
}