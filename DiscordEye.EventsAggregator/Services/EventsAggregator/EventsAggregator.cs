using DiscordEye.EventsAggregator.Services.NodeCommunicateService;
using Grpc.Core;

namespace DiscordEye.EventsAggregator.Services.EventsAggregator;

public class EventsAggregator : EventsAggregatorGrpc.EventsAggregatorGrpcBase
{
    private readonly ILogger<EventsAggregator> _logger;
    private readonly INodeCommunicateService _nodeCommunicateService;

    public EventsAggregator(
        ILogger<EventsAggregator> logger,
        INodeCommunicateService nodeCommunicateService)
    {
        _logger = logger;
        _nodeCommunicateService = nodeCommunicateService;
    }

    public override Task<RegisterResponse> RegisterNode(RegisterRequest request, ServerCallContext context)
    {
        var operationResult = _nodeCommunicateService.CreateGrpcChannel(request.NodeAddress);
        if (operationResult == false)
        {
            _logger.LogWarning($"Failed to register node with address {request.NodeAddress}");
        }
        else
        {
            _logger.LogInformation($"Node with address {request.NodeAddress} was successfully registered");            
        }

        return Task.FromResult(new RegisterResponse
        {
            Result = operationResult ? RegisterResult.Success : RegisterResult.Fail
        });
    }
}