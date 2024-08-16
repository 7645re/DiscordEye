namespace DiscordEye.EventsAggregator.Services.DiscordUserService;

public interface IDiscordUserService
{
    Task AddUserForTrackAsync(
        ulong id,
        CancellationToken cancellationToken = default);
}