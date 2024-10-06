using DiscordEye.Node.Data;

namespace DiscordEye.Node.Services.ProxyHolder;

public interface IProxyHolderService
{
    Task<Proxy?> GetCurrentHoldProxy();
    Task<Proxy?> ReserveProxy();
    Task<bool> ReleaseProxy();
    Task<Proxy?> ReserveProxyWithRetries(
        int retryCount = 1,
        int millisecondsDelay = 100,
        CancellationToken cancellationToken = default);
}