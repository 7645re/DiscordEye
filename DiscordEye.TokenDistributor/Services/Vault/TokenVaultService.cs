using DiscordEye.Infrastructure.Services.Vault;
using DiscordEye.TokenDistributor.Dto;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace DiscordEye.TokenDistributor.Services.Vault;

public class TokenVaultService : VaultService
{
    private const string TokenPath = "token";
    
    public TokenVaultService(IKeyValueSecretsEngineV2 engine, ILogger<VaultService> logger) : base(engine, logger)
    {
    }

    public async Task<List<TokenVault>> GetTokensAsync()
    {
        var proxiesVault = new List<TokenVault>();
        var rows = await GetAllRowsAsync(TokenPath);
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