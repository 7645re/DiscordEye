using DiscordEye.Infrastructure.Services.Files;
using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.FileManagers;

public interface IProxyStateFileManager : IFileManager<ProxyState>
{
    Task RemoveByReleaseKey(Guid releaseKey);
}
