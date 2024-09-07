using System.Net;
using Discord.WebSocket;
using DiscordEye.Node.Dto;

namespace DiscordEye.Node.DiscordClientWrappers.RequestClient;

public interface IDiscordRequestClient
{
    Task<DiscordUser?> GetUserAsync(ulong id);
    Task<DiscordGuild?> GetGuildAsync(ulong id);
    Task<DiscordSocketClient> InitClientAsync(WebProxy? webProxy = null);
}