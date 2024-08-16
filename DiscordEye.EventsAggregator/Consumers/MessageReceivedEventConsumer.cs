using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DiscordEye.EventsAggregator.Consumers;

public class MessageReceivedEventConsumer : IConsumer<MessageReceivedEvent>
{
    private readonly ILogger<MessageReceivedEventConsumer> _logger;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IEventService _eventService;
    
    public MessageReceivedEventConsumer(
        ILogger<MessageReceivedEventConsumer> logger,
        ApplicationDbContext applicationDbContext,
        IEventService eventService)
    {
        _logger = logger;
        _applicationDbContext = applicationDbContext;
        _eventService = eventService;
    }

    public async Task Consume(ConsumeContext<MessageReceivedEvent> context)
    {
        // await _eventService.AddReceivedMessageAsync(context.Message, context.CancellationToken);
    }
}