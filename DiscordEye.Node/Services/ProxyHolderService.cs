using DiscordEye.Node.Data;
using DiscordEye.Node.Mappers;
using DiscordEye.ProxyDistributor;
using DiscordEye.Shared.Extensions;

namespace DiscordEye.Node.Services;

public class ProxyHolderService : IProxyHolderService
{
    private readonly ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient _distributorGrpcServiceClient;
    private Proxy? _holdProxy;
    private readonly string _address = $"localhost:{StartupExtensions.GetPort()}";
    private readonly ILogger<ProxyHolderService> _logger;

    public ProxyHolderService(
        ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient distributorGrpcServiceClient,
        ILogger<ProxyHolderService> logger)
    {
        _distributorGrpcServiceClient = distributorGrpcServiceClient;
        _logger = logger;
    }

    public Proxy? GetCurrentHoldProxy()
    {
        return _holdProxy;
    }
    
    public async Task<Proxy?> ReserveProxyAndReleaseIfNeeded()
    {
        if (_holdProxy is null) return await ReserveProxy();

        var releaseResult = await ReleaseProxy();
        if (releaseResult == false)
        {
            return null;
        }

        return await ReserveProxy();
    }
    
    private async Task<Proxy?> ReserveProxy()
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

    private async Task<bool> ReleaseProxy()
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