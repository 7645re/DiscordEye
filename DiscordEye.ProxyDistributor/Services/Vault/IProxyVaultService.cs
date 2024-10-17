using DiscordEye.ProxyDistributor.Dto;

namespace DiscordEye.ProxyDistributor.Services.Vault;

public interface IProxyVaultService
{
    Task<List<ProxyVault>> GetProxiesAsync();
}