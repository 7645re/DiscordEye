using DiscordEye.DiscordListener;
using DiscordEye.Node.BackgroundServices;
using DiscordEye.Node.Mappers;
using DiscordEye.Shared.Extensions;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class DiscordListenerService : DiscordListener.DiscordListener.DiscordListenerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DiscordListenerService(IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public override async Task<DiscordUserGrpcResponse> GetUser(DiscordUserGrpcRequest request, ServerCallContext context)
    {
        // TODO: maybe set background service to field from service provider
        var remoteIpAddress = _httpContextAccessor.HttpContext.Connection.LocalIpAddress?.ToString();
        var remotePort = _httpContextAccessor.HttpContext.Connection.LocalPort;
        var discordFacade = _serviceProvider.GetHostedService<DiscordFacadeBackgroundService>();
        var discordUser = await discordFacade.GetUserAsync(request.UserId);
        

        var headers = context.ResponseTrailers;
        headers.Add("X-Server-IP", remoteIpAddress);
        headers.Add("X-Server-Port", remotePort.ToString());
        if (discordUser == null)
            return new DiscordUserGrpcResponse
            {
                ErrorMessage = "User not found"
            };

        return new DiscordUserGrpcResponse
        {
            User = discordUser.ToDiscordUserGrpc()
        };
    }

    public override async Task<DiscordGuildGrpcResponse> GetGuild(DiscordGuildGrpcRequest request, ServerCallContext context)
    {
        var discordFacade = _serviceProvider.GetHostedService<DiscordFacadeBackgroundService>();
        var discordGuild = await discordFacade.GetGuildAsync(request.GuildId);
        
        if (discordGuild == null)
            return new DiscordGuildGrpcResponse
            {
                ErrorMessage = "Guild not found"
            };

        return new DiscordGuildGrpcResponse
        {
            Guild = discordGuild.ToDiscordGuildGrpc()
        };
    }
}