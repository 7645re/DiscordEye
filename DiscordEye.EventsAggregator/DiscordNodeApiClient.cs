using System.Net;
using DiscordEye.Shared.DiscordListenerApi.Response;
using Microsoft.Extensions.Options;

namespace DiscordEye.EventsAggregator;

public class DiscordNodeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordNodeApiClient> _logger;

    public DiscordNodeApiClient(
        HttpClient httpClient, ILogger<DiscordNodeApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DiscordUserResponse?> GetUserFromAllNodesAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var userResponses = new List<DiscordUserResponse>();
        foreach (var address in _nodeAddresses)
        {
            var nodeId = int.Parse(address.Key);
            var userResponse = await GetUserAsync(nodeId, userId, cancellationToken);
            if (userResponse != null) userResponses.Add(userResponse);
        }

        return DiscordNodeApiClientHelper.ConcatUsersFromNodes(userResponses);
    }
    
    public async Task<DiscordUserResponse?> GetUserAsync(
        int nodeId,
        long userId,
        CancellationToken cancellationToken)
    {
        var nodeHost = GetHostByNodeId(nodeId);
        var endpoint = $"http://{nodeHost}/discord/users/{userId}";
        return await SendRequestAsync<DiscordUserResponse?>(endpoint, cancellationToken);
    }

    public async Task<DiscordChannelResponse?> GetChannelAsync(
        int nodeId,
        long channelId,
        CancellationToken cancellationToken)
    {
        var nodeHost = GetHostByNodeId(nodeId);
        var endpoint = $"http://{nodeHost}/discord/channels/{channelId}";
        return await SendRequestAsync<DiscordChannelResponse>(endpoint, cancellationToken);
    }
    
    public async Task<DiscordGuildResponse?> GetGuildAsync(
        int nodeId,
        long guildId,
        CancellationToken cancellationToken)
    {
        var nodeHost = GetHostByNodeId(nodeId);
        var endpoint = $"http://{nodeHost}/discord/guilds/{guildId}";
        return await SendRequestAsync<DiscordGuildResponse>(endpoint, cancellationToken);
    }

    private async Task<T?> SendRequestAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<T>(endpoint, cancellationToken);
            return response;
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Resource not found: {endpoint}");
                return default;
            }
            _logger.LogError(e, $"Error occurred while sending request to {endpoint}");
            return default;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unexpected error occurred while processing request to {endpoint}");
            throw;
        }
    }

    private string GetHostByNodeId(int nodeId)
    {
        if (!_nodeAddresses.TryGetValue(nodeId.ToString(), out var nodeHost))
            throw new ArgumentException($"Node with id {nodeId} doesn't exist");

        return nodeHost;
    }
}
