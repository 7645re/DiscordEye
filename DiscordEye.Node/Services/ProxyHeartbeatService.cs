using DiscordEye.ProxyDistributor;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class ProxyHeartbeatService : ProxyHeartbeatGrpcService.ProxyHeartbeatGrpcServiceBase
{
    private readonly IProxyHolderService _proxyHolderService;

    public ProxyHeartbeatService(IProxyHolderService proxyHolderService)
    {
        _proxyHolderService = proxyHolderService;
    }

    public override async Task<ProxyHeartbeatResponse> Heartbeat(ProxyHeartbeatRequest request, ServerCallContext context)
    {
        var holdProxy = await _proxyHolderService.GetCurrentHoldProxy();
        return new ProxyHeartbeatResponse
        {
            ReleaseKey = holdProxy is null ? string.Empty : holdProxy.ReleaseKey.ToString()
        };
    }
}