using DiscordEye.Infrastructure.Services.Files;
using DiscordEye.ProxyDistributor.Services.Node.Data;

namespace DiscordEye.ProxyDistributor.Services.Node.Manager;

public interface ITakerNodesManager : IFileManager<NodeInfoData>
{
    Task RemoveByReleaseKeyKey(Guid releaseKey);
}
