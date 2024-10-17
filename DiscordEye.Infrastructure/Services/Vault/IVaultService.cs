namespace DiscordEye.Infrastructure.Services.Vault;

public interface IVaultService
{
    Task<List<IDictionary<string, object>>> GetAllRowsAsync(string path);
    Task<string[]> GetSecretKeysAsync(string path);
    Task<IDictionary<string, object>?> GetSecretByPathAsync(string path);
}