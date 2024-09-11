using DiscordEye.Infrastructure.Services.Files;
using DiscordEye.ProxyDistributor.Services.Node.Data;

namespace DiscordEye.ProxyDistributor.Services.Node.Manager;

public class TakerNodesManager : BaseJsonFileManager<NodeInfoData>, ITakerNodesManager
{
    protected override string FolderPath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TakerNodes");

    public async Task RemoveByReleaseKey(Guid releaseKey)
    {
        await RemoveBy(x => x.ReleaseKey == releaseKey);
    }
}
