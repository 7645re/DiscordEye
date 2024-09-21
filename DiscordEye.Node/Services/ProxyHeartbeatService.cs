using DiscordEye.Node.DiscordClientWrappers.RequestClient;
using DiscordEye.ProxyDistributor;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class ProxyHeartbeatService : ProxyHeartbeatGrpcService.ProxyHeartbeatGrpcServiceBase
{
    private readonly IDiscordRequestClient _discordRequestClient;

    public ProxyHeartbeatService(IDiscordRequestClient discordRequestClient)
    {
        _discordRequestClient = discordRequestClient;
    }

    public override Task<ProxyHeartbeatResponse> Heartbeat(ProxyHeartbeatRequest request, ServerCallContext context)
    {
        if (!_discordRequestClient.TryGetReleaseKey(out var releaseKey))
        {
            return Task.FromResult(new ProxyHeartbeatResponse
            {
                ReleaseKey = Guid.Empty.ToString()
            });
        }

        return Task.FromResult(new ProxyHeartbeatResponse
        {
            ReleaseKey = releaseKey?.ToString()
        });
    }
}