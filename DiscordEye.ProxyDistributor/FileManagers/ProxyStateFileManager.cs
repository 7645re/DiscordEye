using DiscordEye.Infrastructure.Services.Files;
using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.FileManagers;

public class ProxyStateFileManager : BaseJsonFileManager<ProxyState>, IProxyStateFileManager
{
    protected override string FilePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Snapshots/proxies.json");

    public async Task RemoveByReleaseKey(Guid releaseKey)
    {
        await RemoveBy(x => x.ReleaseKey == releaseKey);
    }
}