using System.Text.Json;
using DiscordEye.ProxyDistributor.Data;

namespace DiscordEye.ProxyDistributor.Services.ProxyStateSnapShooter
{
    public class ProxyStateSnapShooter : IProxyStateSnapShooter
    {
        private readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private const string SnapShootFile = "ProxyStateSnapshot.json";
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public async Task<bool> SnapShootAsync(IDictionary<Guid, ProxyState?> proxyStates)
        {
            ArgumentNullException.ThrowIfNull(proxyStates);

            try
            {
                var filePath = Path.Combine(_rootPath, SnapShootFile);
                var jsonData = JsonSerializer.Serialize(proxyStates, JsonSerializerOptions);
                await File.WriteAllTextAsync(filePath, jsonData);
                return true;
            }
            catch (Exception ex)
            {
                //TODO: Log
                return false;
            }
        }

        public async Task<IDictionary<Guid, ProxyState?>> LoadSnapShotAsync()
        {
            var filePath = Path.Combine(_rootPath, SnapShootFile);

            if (!File.Exists(filePath))
            {
                return new Dictionary<Guid, ProxyState?>();
            }

            try
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                var proxyStates = JsonSerializer.Deserialize<Dictionary<Guid, ProxyState?>>(
                    jsonData,
                    JsonSerializerOptions);
                return proxyStates ?? new Dictionary<Guid, ProxyState?>();
            }
            catch (Exception ex)
            {
                // TODO: Log
                return new Dictionary<Guid, ProxyState?>();
            }
        }
    }
}
