using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.ProxyVault;

public interface IProxyVaultService
{
    Task<Dto.ProxyVault[]> GetAllProxiesAsync();
}