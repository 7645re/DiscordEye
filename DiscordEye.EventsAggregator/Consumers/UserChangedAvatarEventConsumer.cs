using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator.Consumers;

public class UserChangedAvatarEventConsumer : IConsumer<UserChangedAvatarEvent>
{
    private readonly ILogger<UserChangedAvatarEventConsumer> _logger;
    private readonly ApplicationDbContext _applicationDbContext;

    public UserChangedAvatarEventConsumer(
        ILogger<UserChangedAvatarEventConsumer> logger,
        ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
    }

    public async Task Consume(ConsumeContext<UserChangedAvatarEvent> context)
    {
        
    }
}