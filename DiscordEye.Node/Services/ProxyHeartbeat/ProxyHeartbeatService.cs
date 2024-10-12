using DiscordEye.Node.Services.ProxyHolder;
using Grpc.Core;

namespace DiscordEye.Node.Services.ProxyHeartbeat;

public class ProxyHeartbeatService : ProxyHeartbeatGrpcService.ProxyHeartbeatGrpcServiceBase
{
    private readonly IProxyHolderService _proxyHolderService;
    private readonly ILogger<ProxyHeartbeatService> _logger;

    public ProxyHeartbeatService(
        IProxyHolderService proxyHolderService,
        ILogger<ProxyHeartbeatService> logger)
    {
        _proxyHolderService = proxyHolderService;
        _logger = logger;
    }

    public override async Task<ProxyHeartbeatResponse> Heartbeat(ProxyHeartbeatRequest request, ServerCallContext context)
    {
        var holdProxy = await _proxyHolderService.GetCurrentHoldProxy();
        _logger.LogInformation($"Replied to proxy heartbeat {holdProxy?.Id}");
        return new ProxyHeartbeatResponse
        {
            ReleaseKey = holdProxy is null ? string.Empty : holdProxy.ReleaseKey.ToString()
        };
    }
}