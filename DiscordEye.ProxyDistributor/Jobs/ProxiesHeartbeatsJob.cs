using DiscordEye.ProxyDistributor.Services.Heartbeat;
using Quartz;

namespace DiscordEye.ProxyDistributor.Jobs;

[DisallowConcurrentExecution]
public class ProxiesHeartbeatsJob : IJob
{
    private readonly IProxyHeartbeatService _proxyHeartbeatService;

    public ProxiesHeartbeatsJob(IProxyHeartbeatService proxyHeartbeatService)
    {
        _proxyHeartbeatService = proxyHeartbeatService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _proxyHeartbeatService.PulseProxiesHeartbeats();
    }
}