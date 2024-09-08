namespace DiscordEye.ProxyDistributor.Services.ProxyVault;

public interface IProxyVaultService
{
    Task<ProxyInfo[]> GetAllProxiesAsync();
}