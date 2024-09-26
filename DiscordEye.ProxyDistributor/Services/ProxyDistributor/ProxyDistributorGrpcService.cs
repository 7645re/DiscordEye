using DiscordEye.ProxyDistributor.Mappers;
using Grpc.Core;

namespace DiscordEye.ProxyDistributor.Services.ProxyDistributor;

public class ProxyDistributorGrpcService : DiscordEye
    .ProxyDistributor
    .ProxyDistributorGrpcService
    .ProxyDistributorGrpcServiceBase
{
    private readonly IProxyDistributorService _proxyDistributorService;

    public ProxyDistributorGrpcService(
        IProxyDistributorService proxyDistributorService)
    {
        _proxyDistributorService = proxyDistributorService;
    }

    public override async Task<ReserveProxyResponse> ReserveProxy(
        ReserveProxyRequest request,
        ServerCallContext context)
    {
        var reservedProxy = await _proxyDistributorService.ReserveProxy(request.NodeAddress);
        return new ReserveProxyResponse
        {
            ReservedProxy = reservedProxy?.ToReservedProxy()
        };
    }

    public override async Task<ReleaseProxyResponse> ReleaseProxy(
        ReleaseProxyRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var parsedProxyId)
            || !Guid.TryParse(request.ReleaseKey, out var parsedReleaseKey))
        {
            return new ReleaseProxyResponse
            {
                OperationResult = false
            };
        }
        
        var releaseResult = await _proxyDistributorService.ReleaseProxy(parsedProxyId, parsedReleaseKey);
        return new ReleaseProxyResponse
        {
            OperationResult = releaseResult
        };
    }
}
