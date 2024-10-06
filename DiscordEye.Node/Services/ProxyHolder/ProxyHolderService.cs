using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.Node.Data;
using DiscordEye.Node.Mappers;
using DiscordEye.ProxyDistributor;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.Node.Services.ProxyHolder;

public class ProxyHolderService : IProxyHolderService
{
    private readonly ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient _distributorGrpcServiceClient;
    private Proxy? _holdProxy;
    private readonly string _address = $"localhost:{StartupExtensions.GetPort()}";
    private readonly ILogger<ProxyHolderService> _logger;
    private readonly KeyedLockService _keyedLockService;

    public ProxyHolderService(
        ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient distributorGrpcServiceClient,
        ILogger<ProxyHolderService> logger,
        KeyedLockService keyedLockService)
    {
        _distributorGrpcServiceClient = distributorGrpcServiceClient;
        _logger = logger;
        _keyedLockService = keyedLockService;
    }

    public async Task<Proxy?> GetCurrentHoldProxy()
    {
        using (await _keyedLockService.LockAsync("proxy"))
        {
            return _holdProxy;
        }
    }

    public async Task<Proxy?> ReserveProxyWithRetries(
        int retryCount = 1,
        int millisecondsDelay = 100,
        CancellationToken cancellationToken = default)
    {
        var retries = 0;
        while (retries <= retryCount && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation($"{retries + 1} retry to reserve proxy");
            var reservedProxy = await ReserveProxy();
            if (reservedProxy is not null)
            {
                return reservedProxy;
            }
            
            retries++;
            await Task.Delay(millisecondsDelay, cancellationToken);
        }

        return null;
    }
    
    public async Task<Proxy?> ReserveProxy()
    {
        using (await _keyedLockService.LockAsync("proxy"))
        {
            var reservedProxyResponse = await _distributorGrpcServiceClient.ReserveProxyAsync(new ReserveProxyRequest
            {
                NodeAddress = _address
            });
            if (reservedProxyResponse.ReservedProxy is null)
            {
                _logger.LogWarning("Failed to reserve proxy");
                return null;
            }
            var proxy = reservedProxyResponse.ReservedProxy.ToProxy();
            _holdProxy = proxy;
            _logger.LogInformation($"Proxy {_holdProxy.Id} successfully reserved");

            return proxy;    
        }
    }

    public async Task<bool> ReleaseProxy()
    {
        using (await _keyedLockService.LockAsync("proxy"))
        {
            if (_holdProxy is null)
            {
                _logger.LogWarning($"Failed to release proxy");
                throw new ArgumentNullException($"You didn't reserved a proxy to release it");
            }
    
            var releaseProxyResponse = await _distributorGrpcServiceClient.ReleaseProxyAsync(new ReleaseProxyRequest
            {
                Id = _holdProxy.Id.ToString(),
                ReleaseKey = _holdProxy.ReleaseKey.ToString()
            });
            if (releaseProxyResponse.OperationResult == false)
            {
                _logger.LogWarning($"Failed to release proxy, distributor service return false operation result");
                return false;
            }
            _logger.LogInformation($"Successfully release proxy {_holdProxy.Id}");
            _holdProxy = null;

            return true;
        }
    }
}