using Discord.WebSocket;

namespace DiscordEye.Node.DiscordClientWrappers.EventClient;

public interface IDiscordEventClient
{
    Task<DiscordSocketClient> InitClientAsync();
}