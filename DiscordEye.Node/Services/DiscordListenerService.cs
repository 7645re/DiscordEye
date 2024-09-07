using DiscordEye.DiscordListener;
using DiscordEye.Node.BackgroundServices;
using DiscordEye.Node.Mappers;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class DiscordListenerService : DiscordListener.DiscordListener.DiscordListenerBase
{
    private readonly IServiceProvider _serviceProvider;

    public DiscordListenerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public override async Task<DiscordUserGrpcResponse> GetUser(DiscordUserGrpcRequest request, ServerCallContext context)
    {
        // TODO: maybe set background service to field from service provider
        var discordFacade = _serviceProvider.GetHostedService<DiscordFacadeBackgroundService>();
        var discordUser = await discordFacade.GetUserAsync(request.UserId);
        
        if (discordUser == null)
            return new DiscordUserGrpcResponse
            {
                ErrorMessage = "User not found"
            };

        return new DiscordUserGrpcResponse
        {
            User = discordUser.ToDiscordUserGrpc(),
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