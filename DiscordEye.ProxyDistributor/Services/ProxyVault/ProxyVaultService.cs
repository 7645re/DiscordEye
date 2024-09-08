using VaultSharp.Core;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace DiscordEye.ProxyDistributor.Services.ProxyVault;

public class ProxyVaultService : IProxyVaultService
{
    private readonly IKeyValueSecretsEngineV2 _engine;
    private const string ProxyPathPrefix = "proxy";
    private readonly ILogger<ProxyVaultService> _logger;

    public ProxyVaultService(
        IKeyValueSecretsEngineV2 engine,
        ILogger<ProxyVaultService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<ProxyInfo[]> GetAllProxiesAsync()
    {
        var proxies = new List<ProxyInfo>();
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
            _logger.LogInformation($"Error when retrieving a list of keys: {ex.Message}");
            return [];
        }
    }

    private async Task<ProxyInfo?> GetProxyByPathAsync(string path, string mountPoint)
    {
        try
        {
            var secret = await _engine.ReadSecretAsync(path, mountPoint: mountPoint);
            var proxyData = secret.Data.Data;
            var proxy = new ProxyInfo
            {
                Address = proxyData["address"].ToString(),
                Port = proxyData["port"].ToString(),
                Login = proxyData["login"].ToString(),
                Password = proxyData["password"].ToString()
            };

            return proxy;
        }
        catch (VaultApiException ex)
        {
            _logger.LogInformation($"Error when receiving proxy along the path {path}: {ex.Message}");
            return null;
        }
    }
}