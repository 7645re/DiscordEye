using DiscordEye.Node.DiscordClientWrappers.RequestClient;
using DiscordEye.Node.Mappers;
using Grpc.Core;

namespace DiscordEye.Node.Services.Node;

public class NodeService : DiscordEye.Node.Node.NodeBase
{
    private readonly IDiscordRequestClient _discordRequestClient;

    public NodeService(IDiscordRequestClient discordRequestClient)
    {
        _discordRequestClient = discordRequestClient;
    }
    
    public override async Task<DiscordUserGrpcResponse> GetUser(DiscordUserGrpcRequest request, ServerCallContext context)
    {
        var discordUser = await _discordRequestClient.GetUserAsync(request.UserId);
        
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
        var discordGuild = await _discordRequestClient.GetGuildAsync(
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