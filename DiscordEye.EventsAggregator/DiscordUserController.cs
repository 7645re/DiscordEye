using DiscordEye.EventsAggregator.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DiscordEye.EventsAggregator;

[ApiController]
[Route("[controller]")]
public class DiscordUserController : ControllerBase
{
    private readonly ApplicationDbContext _applicationDbContext;

    public DiscordUserController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }
}