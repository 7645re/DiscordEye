using DiscordEye.Node.Data;

namespace DiscordEye.Node.Services;

public interface IProxyHolderService
{
    Task<Proxy?> ReserveProxyAndReleaseIfNeeded();
    Proxy? GetCurrentHoldProxy();
}