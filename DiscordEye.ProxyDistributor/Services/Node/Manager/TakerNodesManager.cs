using DiscordEye.Infrastructure.Services.Files;
using DiscordEye.ProxyDistributor.Services.Node.Data;

namespace DiscordEye.ProxyDistributor.Services.Node.Manager;

public class TakerNodesManager : BaseJsonFileManager<NodeInfoData>, ITakerNodesManager
{
    protected override string FilePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TakerNodes/Nodes.json");

    public async Task RemoveByReleaseKey(Guid releaseKey)
    {
        await RemoveBy(x => x.ReleaseKey == releaseKey);
    }
}
