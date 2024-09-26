using System.Text.Json;

namespace DiscordEye.ProxyDistributor.Services.SnapShoot;

public abstract class SnapShooterBase<T>
{
    private readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
    private readonly string _snapShootFile;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<SnapShooterBase<T>> _logger;

    protected SnapShooterBase(string snapShootFileName, ILogger<SnapShooterBase<T>> logger)
    {
        _snapShootFile = snapShootFileName;
        _logger = logger;
    }

    public async Task<bool> SnapShootAsync(T data)
    {
        try
        {
            var filePath = Path.Combine(_rootPath, _snapShootFile);
            var jsonData = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, jsonData);
            _logger.LogInformation($"SnapShoot {_snapShootFile}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error saving snapShoot {_snapShootFile}");
            return false;
        }
    }

    public async Task<T?> LoadSnapShotAsync()
    {
        var filePath = Path.Combine(_rootPath, _snapShootFile);

        if (!File.Exists(filePath))
        {
            return default;
        }

        try
        {
            var jsonData = await File.ReadAllTextAsync(filePath);
            if (jsonData == string.Empty)
            {
                return default;
            }
            
            var data = JsonSerializer.Deserialize<T>(jsonData, _jsonSerializerOptions);
            _logger.LogInformation($"SnapShoot loaded from {_snapShootFile}");
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error loading snapShoot from {_snapShootFile}");
            return default;
        }
    }
}