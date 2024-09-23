using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class ProxyHeartbeatService : ProxyHeartbeatGrpcService.ProxyHeartbeatGrpcServiceBase
{
    private readonly KeyedLockService _lockService;
    private readonly IProxyHolderService _proxyHolderService;

    public ProxyHeartbeatService(
        KeyedLockService lockService,
        IProxyHolderService proxyHolderService)
    {
        _lockService = lockService;
        _proxyHolderService = proxyHolderService;
    }

    public override async Task<ProxyHeartbeatResponse> Heartbeat(ProxyHeartbeatRequest request, ServerCallContext context)
    {
        using (await _lockService.LockAsync("ProxyHoldMutation"))
        {
            var holdProxy = _proxyHolderService.GetCurrentHoldProxy();
            return new ProxyHeartbeatResponse
            {
                ReleaseKey = holdProxy is null ? string.Empty : holdProxy.ReleaseKey.ToString()
            };
        }
    }
}