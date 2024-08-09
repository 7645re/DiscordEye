using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator.Consumers;

public class UserBannedEventConsumer : IConsumer<UserBannedEvent>
{
    private readonly ILogger<UserBannedEventConsumer> _logger;
    private readonly ApplicationDbContext _applicationDbContext;

    public UserBannedEventConsumer(
        ILogger<UserBannedEventConsumer> logger,
        ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
    }

    public async Task Consume(ConsumeContext<UserBannedEvent> context)
    {
        
    }
}