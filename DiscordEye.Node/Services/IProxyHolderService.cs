using DiscordEye.Node.Data;

namespace DiscordEye.Node.Services;

public interface IProxyHolderService
{
    Task<Proxy?> GetCurrentHoldProxy();
    Task<Proxy?> ReserveProxy();
    Task<bool> ReleaseProxy();
    Task<Proxy?> ReserveProxyWithRetries(
        int retryCount = 1,
        int millisecondsDelay = 0,
        CancellationToken cancellationToken = default);
}