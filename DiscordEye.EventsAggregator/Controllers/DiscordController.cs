using DiscordEye.EventsAggregator.Services.NodeCommunicateService;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.EventsAggregator.Controllers;

[ApiController]
[Route("discord")]
public class DiscordController : ControllerBase
{
    private readonly INodeCommunicateService _nodeCommunicateService;
    
    public DiscordController(INodeCommunicateService nodeCommunicateService)
    {
        _nodeCommunicateService = nodeCommunicateService;
    }

    [HttpGet("users/{id:long}")]
    public async Task<IActionResult> GetUser(ulong id, CancellationToken cancellationToken)
    {
        var user = await _nodeCommunicateService.GetAggregatedDiscordUser(id, cancellationToken);
        return Ok(user);
    }
}