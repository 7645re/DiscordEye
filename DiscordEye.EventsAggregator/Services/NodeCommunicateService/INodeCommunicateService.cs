using DiscordEye.EventsAggregator.Dto;

namespace DiscordEye.EventsAggregator.Services.NodeCommunicateService;

public interface INodeCommunicateService
{
    Task<DiscordUser> GetAggregatedDiscordUser(ulong userId, CancellationToken cancellationToken = default);
    public bool CreateGrpcChannel(string address);
}
