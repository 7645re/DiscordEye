using Microsoft.Extensions.Logging;
using VaultSharp.Core;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace DiscordEye.Infrastructure.Services.Vault;

public class VaultService : IVaultService
{
    private readonly IKeyValueSecretsEngineV2 _engine;
    private readonly ILogger<VaultService> _logger;
    private const string SecretMountPoint = "secret";
    
    public VaultService(
        IKeyValueSecretsEngineV2 engine,
        ILogger<VaultService> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    public async Task<List<IDictionary<string, object>>> GetAllRowsAsync(string path)
    {
        var secrets = new List<IDictionary<string, object>>();
        var secretKeys = await GetSecretKeysAsync(path);
        foreach (var secretKey in secretKeys)
        {
            var pathToResource = $"{path}/{secretKey}";
            var secret = await GetSecretByPathAsync(pathToResource);
            if (secret != null) secrets.Add(secret);
        }

        return secrets;
    }

    public async Task<string[]> GetSecretKeysAsync(string path)
    {
        try
        {
            return (await _engine.ReadSecretPathsAsync(
                $"{path}/",
                SecretMountPoint))
                .Data
                .Keys
                .ToArray();
        }
        catch (VaultApiException)
        {
            _logger.LogError($"Data was not found in Vault along {path}" +
                             $" with mount point {SecretMountPoint}");
            return Array.Empty<string>();
        }
    }

    public async Task<IDictionary<string, object>?> GetSecretByPathAsync(string path)
    {
        try
        {
            var secret = await _engine.ReadSecretAsync(path, mountPoint: SecretMountPoint);
            _logger.LogInformation($"Data was retrieved from Vault along " +
                                   $"the path {SecretMountPoint + "/" + path}");
            return secret.Data.Data;
        }
        catch (VaultApiException ex)
        {
            _logger.LogError($"Data was not found in Vault at {path} with mount point {SecretMountPoint}");
            return null;
        }
    }
}