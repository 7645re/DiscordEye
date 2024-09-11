using System.Collections.Concurrent;
using DiscordEye.ProxyDistributor.Data;
using DiscordEye.ProxyDistributor.Services.ProxyStorage;
using Grpc.Core;
using Grpc.Net.Client;

namespace DiscordEye.ProxyDistributor.BackgroundServices;

public class ProxyHeartbeatBackgroundService : BackgroundService
{
    private readonly ConcurrentQueue<Proxy> _takenProxies = new();
    private readonly ILogger<ProxyHeartbeatBackgroundService> _logger;
    private readonly IProxyStorageService _proxyStorageService;
    private readonly ConcurrentDictionary<string, GrpcChannel> _cachedChannels = new();

    //TODO: Transfer heartbeat periods and check period to configuration file
    private readonly TimeSpan _heartbeatPeriod = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _proxiesAvailableCheckPeriod = TimeSpan.FromSeconds(1);

    public ProxyHeartbeatBackgroundService(
        ILogger<ProxyHeartbeatBackgroundService> logger,
        IProxyStorageService proxyStorageService
    )
    {
        _logger = logger;
        _proxyStorageService = proxyStorageService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartProxiesAvailableCheckTask(stoppingToken);
    }

    public bool TryRegisterProxy(Proxy proxy)
    {
        if (proxy.IsFree())
        {
            _logger.LogInformation($"Can't register proxy with id {proxy.Id}, because its free");
            return false;
        }

        _takenProxies.Enqueue(proxy);
        _logger.LogInformation($"Proxy with id {proxy.Id} registered to heartbeat service");
        return true;
    }

    private async Task StartProxiesAvailableCheckTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (!_takenProxies.IsEmpty)
            {
                if (
                    !_takenProxies.TryDequeue(out var proxy)
                    || proxy.IsFree()
                    || proxy.TakerAddress is null
                    || proxy.TakenDateTime is null
                )
                    continue;

                if (DateTime.Now.Subtract(proxy.TakenDateTime.Value) < _heartbeatPeriod)
                {
                    _takenProxies.Enqueue(proxy);
                    continue;
                }

                var result = await HeartbeatToTakerAsync(proxy);
                if (result.heartbeatResult)
                {
                    _logger.LogInformation(
                        $"Success heartbeat to node {proxy.TakerAddress}"
                            + $" for proxy with id {proxy.Id}"
                    );
                    if (
                        await _proxyStorageService.TryProlong(
                            result.releaseKey.Value,
                            _heartbeatPeriod,
                            proxy
                        )
                    )
                    {
                        _logger.LogInformation($"Prolonged proxy with id {proxy.Id}");
                        _takenProxies.Enqueue(proxy);
                    }
                    else
                    {
                        _logger.LogWarning($"Can't prolong proxy with id {proxy.Id}");
                    }
                }
                else
                {
                    var takerAddress = proxy.TakerAddress;
                    _logger.LogWarning($"Failed heartbeat to node {takerAddress}");
                    if (await _proxyStorageService.TryForceReleaseProxy(proxy.Id))
                        _logger.LogInformation(
                            $"Force released proxy with id {proxy.Id},"
                                + $" because node {takerAddress} dont answer to heartbeat"
                        );
                    else
                        _logger.LogWarning($"Can't force release proxy with id {proxy.Id}");
                }
            }
            await Task.Delay(_proxiesAvailableCheckPeriod, cancellationToken);
        }
    }

    private bool TryCreateOrGetCachedGrpcChannel(string address, out GrpcChannel? createdChannel)
    {
        if (_cachedChannels.TryGetValue(address, out var channel))
        {
            _logger.LogInformation($"Get cached grpc channel for {address}");
            createdChannel = channel;
            return true;
        }

        var newChannel = GrpcChannel.ForAddress($"http://{address}");
        if (!_cachedChannels.TryAdd(address, newChannel))
        {
            createdChannel = null;
            return false;
        }

        _logger.LogInformation($"Created new grpc channel for {address}");
        createdChannel = newChannel;
        return true;
    }

    private async Task<(bool heartbeatResult, Guid? releaseKey)> HeartbeatToTakerAsync(Proxy proxy)
    {
        (bool heartbeatResult, Guid? releaseKey) result = (false, null);

        // TODO: validate node address by regex
        if (
            proxy.TakerAddress is null
            || !TryCreateOrGetCachedGrpcChannel(proxy.TakerAddress, out var channel)
        )
        {
            return (false, null);
        }

        var client = new ProxyHeartbeat.ProxyHeartbeatClient(channel);
        try
        {
            var response = await client.HeartbeatAsync(new ProxyHeartbeatRequest());
            if (
                response.ReleaseKey is not null
                && Guid.TryParse(response.ReleaseKey, out var parsedReleaseKey)
                && proxy.EqualsReleaseKey(parsedReleaseKey)
            )
            {
                result.heartbeatResult = true;
                result.releaseKey = parsedReleaseKey;
            }
        }
        catch (RpcException) { }

        return result;
    }
}
