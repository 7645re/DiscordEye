using DiscordEye.EventsAggregator.Services.DiscordUserService;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.EventsAggregator.Controllers;

[ApiController]
[Route("discord")]
public class DiscordUserController : ControllerBase
{
    private readonly IDiscordUserService _discordUserService;
    
    public DiscordUserController(
        IDiscordUserService discordUserService)
    {
        _discordUserService = discordUserService;
    }

    [HttpGet("users/{id:long}/add-for-track")]
    public async Task<IActionResult> AddUserForTrack(ulong id, CancellationToken cancellationToken)
    {
        await _discordUserService.AddUserForTrackAsync(id, cancellationToken);
        return Ok();
    }
}