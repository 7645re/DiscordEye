using DiscordEye.ProxyDistributor.Dto;
using DiscordEye.ProxyDistributor.Mappers;
using VaultSharp.Core;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace DiscordEye.ProxyDistributor.Services.Vault;

public class ProxyVaultService : IProxyVaultService
{
    private const string ProxyPathPrefix = "proxy";
    private readonly IKeyValueSecretsEngineV2 _engine;
        
    public ProxyVaultService(
        IKeyValueSecretsEngineV2 engine)
    {
        _engine = engine;
    }

    public async Task<ProxyVault[]> GetAllProxiesAsync()
    { 
        var proxies = new List<ProxyVault>();
        var proxyKeys = await GetProxyKeysAsync();
        foreach (var path in proxyKeys.Select(key => $"{ProxyPathPrefix}/{key}"))
        {
            var proxy = await GetProxyByPathAsync(path, mountPoint: "secret");

            if (proxy != null)
                proxies.Add(proxy);
        }

        return proxies.ToArray();
    }

    private async Task<List<string>> GetProxyKeysAsync()
    {
        try
        {
            var listResponse = await _engine.ReadSecretPathsAsync(
                $"{ProxyPathPrefix}/",
                "secret");
            var keys = listResponse.Data.Keys;

            return keys.ToList();
        }
        catch (VaultApiException ex)
        {
            return [];
        }
    }

    private async Task<ProxyVault?> GetProxyByPathAsync(string path, string mountPoint)
    {
        try
        {
            var secret = await _engine.ReadSecretAsync(path, mountPoint: mountPoint);
            var secretData = secret.Data.Data;
            if (secretData is null)
                return null;

            if (!secretData.TryToProxyVault(out var proxy))
                return null;
            
            return proxy;
        }
        catch (VaultApiException ex)
        {
            return null;
        }
    }
}