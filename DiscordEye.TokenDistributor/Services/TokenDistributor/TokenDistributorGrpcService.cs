using Grpc.Core;

namespace DiscordEye.TokenDistributor.Services.TokenDistributor;

public class TokenDistributorGrpcService : TokenDistributorGrpc.TokenDistributorGrpcBase
{
    public override Task<ReserveTokenResponse> ReserveToken(ReserveTokenRequest request, ServerCallContext context)
    {
        return base.ReserveToken(request, context);
    }
}