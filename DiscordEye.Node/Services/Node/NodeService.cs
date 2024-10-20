using DiscordEye.Node.Mappers;
using DiscordEye.Node.Services.DiscordClient;
using Grpc.Core;

namespace DiscordEye.Node.Services.Node;

public class NodeService : NodeGrpc.NodeGrpcBase
{
    private readonly IDiscordClientService _discordClientService;

    public NodeService(IDiscordClientService discordClientService)
    {
        _discordClientService = discordClientService;
    }
    
    public override async Task<DiscordUserGrpcResponse> GetUser(DiscordUserGrpcRequest request, ServerCallContext context)
    {
        var discordUser = await _discordClientService.GetUserAsync(request.UserId);
        
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

    public override async Task<DiscordGuildGrpcResponse> GetGuild(
        DiscordGuildGrpcRequest request,
        ServerCallContext context)
    {
        var discordGuild = await _discordClientService.GetGuildAsync(
            request.GuildId,
            request.WithChannels);
        
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