using System.Collections.Concurrent;
using DiscordEye.EventsAggregator.Dto;
using DiscordEye.EventsAggregator.Mappers;
using Grpc.Net.Client;

namespace DiscordEye.EventsAggregator.Services.NodeCommunicateService;

public class NodeCommunicateService : INodeCommunicateService, IDisposable
{
    private readonly ConcurrentDictionary<string, (GrpcChannel channel, NodeGrpc.NodeGrpcClient client)>
        _cachedGrpcChannel;
    private readonly ILogger<NodeCommunicateService> _logger;

    public NodeCommunicateService(ILogger<NodeCommunicateService> logger)
    {
        _logger = logger;
        _cachedGrpcChannel = new ConcurrentDictionary<string, (GrpcChannel channel, NodeGrpc.NodeGrpcClient client)>();
    }

    public async Task<DiscordUser> GetAggregatedDiscordUser(ulong userId, CancellationToken cancellationToken = default)
    {
        var discordUsersForAggregate = new List<DiscordUser>();
        foreach (var keyValuePair in _cachedGrpcChannel)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = keyValuePair.Value.client;
            var discordUserGrpcResponse = await client.GetUserAsync(new DiscordUserGrpcRequest
            {
                UserId = userId
            }, cancellationToken: cancellationToken);

            if (discordUserGrpcResponse.User is null)
            {
                _logger.LogWarning($"Node with the address {keyValuePair.Key} could not return information about the user with ID {userId}");
                continue;
            }

            var discordUser = discordUserGrpcResponse.User.ToDiscordUser();
            discordUsersForAggregate.Add(discordUser);
        }

        return AggregateDiscordUserVariants(discordUsersForAggregate);
    }
    
    public bool CreateGrpcChannel(string address)
    {
        if (_cachedGrpcChannel.TryGetValue(address, out _))
        {
            _logger.LogInformation($"Grpc channel has already been created for the address {address}");
            return false;
        }

        var grpcChannel = CreateGrpcChannelInternal($"http://{address}");
        if (grpcChannel is null)
        {
            _logger.LogWarning($"Failed to create grpc channel for address {address}");
            return false;
        }

        var grpcClient = new NodeGrpc.NodeGrpcClient(grpcChannel);
        if (!_cachedGrpcChannel.TryAdd(address, (grpcChannel, grpcClient)))
        {
            _logger.LogWarning($"Failed to add grpc channel for address {address}");
            return false;
        }
        
        _logger.LogInformation($"Created grpc channel for {address}");
        return true;
    }
    
    private GrpcChannel? CreateGrpcChannelInternal(string uri)
    {
        try
        {
            var channel = GrpcChannel.ForAddress(uri);
            return channel;
        }
        catch (Exception e)
        {
            _logger.LogWarning("An error occurred while creating an grpc channel for address {uri}", e);
            return null;
        }
    }
    
    private static DiscordUser AggregateDiscordUserVariants(List<DiscordUser> discordUsersForAggregate)
    {
        switch (discordUsersForAggregate.Count)
        {
            case 0:
                throw new ArgumentException("Cannot concat less than one element");
            case 1:
                return discordUsersForAggregate.First();
        }

        var aggregatedGuilds = new List<DiscordGuild>();
        
        foreach (var user in discordUsersForAggregate)
        {
            aggregatedGuilds.AddRange(user.Guilds);
        }

        var firstDiscordUser = discordUsersForAggregate.First();
        var uniqueAggregatedGuilds = aggregatedGuilds.DistinctBy(x => x.Id);
        return new DiscordUser
        {
            Id = firstDiscordUser.Id,
            Username = firstDiscordUser.Username,
            Guilds = uniqueAggregatedGuilds.ToArray()
        };
    }

    public void Dispose()
    {
        foreach (var keyValuePair in _cachedGrpcChannel)
        {
            keyValuePair.Value.channel.Dispose();
            _logger.LogInformation($"Grpc channel for address {keyValuePair.Key} has been disposed");
        }
    }
}