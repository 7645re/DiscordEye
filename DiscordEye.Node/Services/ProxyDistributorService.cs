using DiscordEye.ProxyDistributor;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class ProxyDistributorService : ProxyDistributor.ProxyDistributorService.ProxyDistributorServiceClient
{
    // public override AsyncUnaryCall<ProxyResponse> TakeProxyAsync(
    //     // ProxyRequest request,
    //     Metadata headers = null,
    //     DateTime? deadline = null,
    //     CancellationToken cancellationToken = default(CancellationToken))
    // {
    //     throw new NotImplementedException();
    //     // return base.TakeProxyAsync(request, headers, deadline, cancellationToken);
    // }
}