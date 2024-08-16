using DiscordEye.DiscordListener.Mappers;
using DiscordEye.Shared.DiscordListenerApi.Response;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.DiscordListener;

[ApiController]
[Route("discord")]
public class DiscordController : ControllerBase
{
    [HttpGet("users/{id:long}")]
    public async Task<DiscordUserResponse> GetUser(ulong id, [FromQuery] bool guildInfo = false)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var userProfile = await backgroundService.GetUserAsync(id, guildInfo);
        return userProfile.ToDiscordUserResponse();
    }

    [HttpGet("guilds/{id:long}")]
    public async Task<DiscordGuildResponse> GetGuild(long id, [FromQuery] bool withChannels)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var guild = backgroundService.GetGuild((ulong)id, withChannels);
        return guild.ToGuildResponse();
    }
}