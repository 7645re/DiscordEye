using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.ProxyStorage;
using Grpc.Core;

namespace DiscordEye.ProxyDistributor.Services;

public class ProxyDistributorService : ProxyDistributor.ProxyDistributorService.ProxyDistributorServiceBase
{
    private readonly IProxyStorageService _proxyStorageService;

    public ProxyDistributorService(IProxyStorageService proxyStorageService)
    {
        _proxyStorageService = proxyStorageService;
    }

    public override Task<TakeProxyResponse> TakeProxy(TakeProxyRequest request, ServerCallContext context)
    {
        if (!_proxyStorageService.TryTakeProxy(out var takenProxyWithKey))
            return Task.FromResult(new TakeProxyResponse
            {
                Proxy = null,
                ErrorMessage = "Failed to take proxy"
            });

        return Task.FromResult(new TakeProxyResponse
        {
            Proxy = takenProxyWithKey.takenProxy.ToProxyInfo(takenProxyWithKey.releaseKey)
        });
    }

    public override Task<ReleaseProxyResponse> ReleaseProxy(ReleaseProxyRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.ReleaseKey, out var releaseKey))
            return Task.FromResult(new ReleaseProxyResponse
            {
                OperationSuccessful = false,
                ErrorMessage = "Cannot parse release key"
            });
        
        if (!_proxyStorageService.TryReleaseProxy(request.ProxyId, releaseKey))
            return Task.FromResult(new ReleaseProxyResponse
            {
                OperationSuccessful = false,
                ErrorMessage = "Failed to release proxy"
            });
        
        return Task.FromResult(new ReleaseProxyResponse
        {
            OperationSuccessful = true
        });
    }
}