using DiscordEye.Node.DiscordClientWrappers.EventClient;
using DiscordEye.Node.DiscordClientWrappers.RequestClient;
using DiscordEye.Node.Dto;

namespace DiscordEye.Node.BackgroundServices;

public class DiscordFacadeBackgroundService : BackgroundService
{
    private readonly IDiscordEventClient _discordEventClient;
    private readonly IDiscordRequestClient _discordRequestClient;
    
    public DiscordFacadeBackgroundService(
        IDiscordEventClient discordEventClient,
        IDiscordRequestClient discordRequestClient)
    {
        _discordEventClient = discordEventClient;
        _discordRequestClient = discordRequestClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }

    public async Task<DiscordUser?> GetUserAsync(ulong id)
    {
        return await _discordRequestClient.GetUserAsync(id);
    }

    public async Task<DiscordGuild?> GetGuildAsync(ulong id)
    {
        return await _discordRequestClient.GetGuildAsync(id);
    }
}