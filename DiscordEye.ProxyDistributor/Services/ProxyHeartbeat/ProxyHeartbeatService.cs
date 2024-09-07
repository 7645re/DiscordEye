using System.Collections.Concurrent;
using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyHeartbeat;

public class ProxyHeartbeatService : IHostedService
{
    private readonly ConcurrentQueue<Proxy> _takenProxies = new();
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public bool RegisterProxy(Proxy proxy)
    {
        if (!proxy.IsFree() && proxy.)
    }
}