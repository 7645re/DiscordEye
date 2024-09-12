using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using Grpc.Core;

namespace DiscordEye.ProxyDistributor.Services.ProxyDistributor;

public class ProxyDistributorService
    : DiscordEye.ProxyDistributor.ProxyDistributorService.ProxyDistributorServiceBase
{
    private readonly IProxyReservationService _proxyReservationService;

    public ProxyDistributorService(IProxyReservationService proxyReservationService)
    {
        _proxyReservationService = proxyReservationService;
    }

    public override Task<GetProxiesResponse> GetProxies(
        GetProxiesRequest request,
        ServerCallContext context
    )
    {
        var proxies = _proxyReservationService.GetProxies();
        return Task.FromResult(
            new GetProxiesResponse { Proxies = {  } }
        );
    }

    // public override async Task<TakeProxyResponse> TakeProxy(
    //     TakeProxyRequest request,
    //     ServerCallContext context
    // )
    // {
    //     if (
    //         string.IsNullOrEmpty(request.NodeAddress)
    //         || string.IsNullOrWhiteSpace(request.NodeAddress)
    //     )
    //         return new TakeProxyResponse
    //         {
    //             Proxy = null,
    //             ErrorMessage = "Cannot take proxy without node address"
    //         };
    //
    //     // var takenProxy = await _proxyStorageService.TryTakeProxy(request.NodeAddress);
    //     // if (takenProxy is null)
    //     // {
    //     //     return new TakeProxyResponse { Proxy = null, ErrorMessage = "Failed to take proxy" };
    //     // }
    //
    //     return new TakeProxyResponse { Proxy = takenProxy.ToTakenProxy() };
    // }

    // public override async Task<ReleaseProxyResponse> ReleaseProxy(
    //     ReleaseProxyRequest request,
    //     ServerCallContext context
    // )
    // {
    //     if (Guid.TryParse(request.ReleaseKey, out var releaseKey) == false)
    //     {
    //         return new ReleaseProxyResponse
    //         {
    //             OperationSuccessful = false,
    //             ErrorMessage = "Cannot parse release key"
    //         };
    //     }
    //
    //     if (await _proxyStorageService.TryReleaseProxy(request.ProxyId, releaseKey) == false)
    //     {
    //         return new ReleaseProxyResponse
    //         {
    //             OperationSuccessful = false,
    //             ErrorMessage = "Failed to release proxy"
    //         };
    //     }
    //
    //     return new ReleaseProxyResponse { OperationSuccessful = true };
    // }
}
