using DiscordEye.Shared.Response;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.DiscordListener;

[ApiController]
[Route("discord")]
public class DiscordController : ControllerBase
{
    [HttpGet("users/{id:long}")]
    public async Task<UserResponse> GetUser(long id)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var userProfile = await backgroundService.GetUserProfileAsync((ulong)id);
        return userProfile.ToDiscordUserResponse();
    }

    [HttpGet("channels/{id:long}")]
    public async Task<ChannelResponse> GetChannel(long id)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var userProfile = await backgroundService.GetChannelAsync((ulong)id);
        return userProfile.ToChannelResponse();
    }
    
    [HttpGet("guilds/{id:long}")]
    public async Task<GuildResponse> GetGuild(long id)
    {
        var backgroundService = HttpContext
            .RequestServices
            .GetHostedService<DiscordListenerBackgroundService>();
        var guild = await backgroundService.GetGuildAsync((ulong)id);
        return guild.ToGuildResponse();
    }
}