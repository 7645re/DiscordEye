using Discord.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DiscordEye.DiscordListener.Filters;

public class CloudFlareExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is not CloudFlareException) return;
        context.Result = new ObjectResult(new { error = context.Exception.Message })
        {
            StatusCode = 429
        };
        context.ExceptionHandled = true;
    }
}