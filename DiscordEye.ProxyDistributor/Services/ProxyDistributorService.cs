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

    public override Task<ProxyResponse> TakeProxy(ProxyRequest request, ServerCallContext context)
    {
        var takenProxy = _proxyStorageService.TakeProxy(request.ServiceName);

        var result = new ProxyResponse();
        if (takenProxy is null)
        {
            result.ErrorMessage = "Proxy not found";
        }
        else
        {
            result.Proxy = takenProxy.ToProxyInfo();
        }

        return Task.FromResult(result);
    }
}