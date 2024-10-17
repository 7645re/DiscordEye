using DiscordEye.Infrastructure.Services.Vault;
using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace DiscordEye.ProxyDistributor.Services.Vault;

public class ProxyVaultService : VaultService, IProxyVaultService
{
    private const string ProxyPath = "proxy";
    
    public ProxyVaultService(IKeyValueSecretsEngineV2 engine, ILogger<VaultService> logger) : base(engine, logger)
    {
    }

    public async Task<List<ProxyVault>> GetProxiesAsync()
    {
        var proxiesVault = new List<ProxyVault>();
        var rows = await GetAllRowsAsync(ProxyPath);
        foreach (var row in rows)
        {
            if (row.TryToProxyVault(out var mappedProxyVault))
            {
                proxiesVault.Add(mappedProxyVault);
            }
        }

        return proxiesVault;
    }
}