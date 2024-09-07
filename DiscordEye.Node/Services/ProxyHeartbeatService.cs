using Grpc.Core;

namespace DiscordEye.Node.Services;

public class ProxyHeartbeatService : ProxyHeartbeat.ProxyHeartbeatBase
{
    public override Task<ProxyHeartbeatResponse> Heartbeat(ProxyHeartbeatRequest request, ServerCallContext context)
    {
        return Task.FromResult(new ProxyHeartbeatResponse());
    }
}