using DiscordEye.DiscordListener.Filters;
using DiscordEye.DiscordListener.Mappers;
using DiscordEye.Shared.DiscordListenerApi.Response;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.DiscordListener;

[ApiController]
[Route("discord")]
[CloudFlareExceptionFilter]
public class DiscordController : ControllerBase
{
    [HttpGet("users/{id:long}")]
    public async Task<DiscordUserResponse> GetUser(ulong id, [FromQuery] bool withGuilds = false)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var userProfile = await backgroundService.GetUserAsync(id, withGuilds);
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