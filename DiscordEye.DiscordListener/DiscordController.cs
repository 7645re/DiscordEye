using DiscordEye.Shared.Response;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.DiscordListener;

[ApiController]
[Route("discord")]
public class DiscordController : ControllerBase
{
    [HttpGet("users/{id:long}")]
    public async Task<IActionResult> GetUser(long id)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var userProfile = await backgroundService.GetUserProfileAsync((ulong)id);
        return userProfile is null ? NotFound() : Ok(userProfile.ToDiscordUserResponse());
    }

    [HttpGet("channels/{id:long}")]
    public async Task<IActionResult> GetChannel(long id)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var channel = await backgroundService.GetChannelAsync((ulong)id);
        return channel is null ? NotFound() : Ok(channel.ToChannelResponse());
    }
    
    [HttpGet("guilds/{id:long}")]
    public async Task<IActionResult> GetGuild(long id)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var guild = await backgroundService.GetGuildAsync((ulong)id);
        return guild is null ? NotFound() : Ok(guild.ToGuildResponse());
    }
}