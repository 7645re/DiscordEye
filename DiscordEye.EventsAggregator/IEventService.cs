using DiscordEye.Shared.Events;

namespace DiscordEye.EventsAggregator;

public interface IEventService
{
    Task AddReceivedMessageAsync(
        MessageReceivedEvent messageReceivedEvent,
        CancellationToken cancellationToken);
}