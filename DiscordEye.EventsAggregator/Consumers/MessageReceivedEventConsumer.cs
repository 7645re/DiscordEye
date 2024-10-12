using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator.Consumers;

public class MessageReceivedEventConsumer : IConsumer<MessageReceivedEvent>
{
    public MessageReceivedEventConsumer()
    {
    }

    public async Task Consume(ConsumeContext<MessageReceivedEvent> context)
    {
    }
}