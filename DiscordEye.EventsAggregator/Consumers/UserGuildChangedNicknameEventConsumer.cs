using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator.Consumers;

public class UserGuildChangedNicknameEventConsumer : IConsumer<UserGuildChangedNicknameEvent>
{
    private readonly ILogger<UserChangedAvatarEventConsumer> _logger;
    private readonly ApplicationDbContext _applicationDbContext;

    public UserGuildChangedNicknameEventConsumer(
        ILogger<UserChangedAvatarEventConsumer> logger,
        ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
    }

    public async Task Consume(ConsumeContext<UserGuildChangedNicknameEvent> context)
    {
        
    }
}