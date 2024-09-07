namespace DiscordEye.ProxyDistributor.Services;

public class ProxyHeartbeatService : IHostedService
{
    private Timer? _timer;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}