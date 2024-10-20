using DiscordEye.Node.Data;

namespace DiscordEye.Node.Services.DiscordClient;

public interface IDiscordClientService
{
    Task<DiscordUser?> GetUserAsync(ulong id);

    Task<DiscordGuild?> GetGuildAsync(
        ulong id,
        bool withChannels = false);
}