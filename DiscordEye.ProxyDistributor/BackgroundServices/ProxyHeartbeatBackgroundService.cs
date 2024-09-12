// using System.Collections.Concurrent;
// using DiscordEye.ProxyDistributor.Data;
// using DiscordEye.ProxyDistributor.Services.ProxyStorage;
// using Grpc.Core;
// using Grpc.Net.Client;
//
// namespace DiscordEye.ProxyDistributor.BackgroundServices;
//
// public class ProxyHeartbeatBackgroundService : BackgroundService
// {
//     private readonly ConcurrentQueue<Proxy> _takenProxies = new();
//     private readonly ILogger<ProxyHeartbeatBackgroundService> _logger;
//     private readonly IProxyStorageService _proxyStorageService;
//     private readonly ConcurrentDictionary<string, GrpcChannel> _cachedChannels = new();
//
//     //TODO: Transfer heartbeat periods and check period to configuration file
//     private readonly TimeSpan _heartbeatPeriod = TimeSpan.FromSeconds(10);
//     private readonly TimeSpan _proxiesAvailableCheckPeriod = TimeSpan.FromSeconds(1);
//
//     public ProxyHeartbeatBackgroundService(
//         ILogger<ProxyHeartbeatBackgroundService> logger,
//         IProxyStorageService proxyStorageService
//     )
//     {
//         _logger = logger;
//         _proxyStorageService = proxyStorageService;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         await StartProxiesAvailableCheckTask(stoppingToken);
//     }
//
//     public bool TryRegisterProxy(Proxy proxy)
//     {
//         if (proxy.IsFree())
//         {
//             return false;
//         }
//
//         _takenProxies.Enqueue(proxy);
//         return true;
//     }
//
//     private async Task StartProxiesAvailableCheckTask(CancellationToken cancellationToken)
//     {
//         while (!cancellationToken.IsCancellationRequested)
//         {
//             while (!_takenProxies.IsEmpty)
//             {
//                 if (
//                     !_takenProxies.TryDequeue(out var proxy)
//                     || proxy.IsFree()
//                     || proxy.TakerAddress is null
//                     || proxy.TakenDateTime is null
//                 )
//                     continue;
//
//                 if (DateTime.Now.Subtract(proxy.TakenDateTime.Value) < _heartbeatPeriod)
//                 {
//                     _takenProxies.Enqueue(proxy);
//                     continue;
//                 }
//
//                 var result = await HeartbeatToTakerAsync(proxy);
//                 if (result.heartbeatResult)
//                 {
//                     if (
//                         await _proxyStorageService.TryProlong(
//                             result.releaseKey.Value,
//                             _heartbeatPeriod,
//                             proxy
//                         )
//                     )
//                     {
//                         _takenProxies.Enqueue(proxy);
//                     }
//                     else
//                     {
//                         _logger.LogWarning($"Can't prolong proxy with id {proxy.Id}");
//                     }
//                 }
//                 else
//                 {
//                     var takerAddress = proxy.TakerAddress;
//                     _logger.LogWarning($"Failed heartbeat to node {takerAddress}");
//                     await _proxyStorageService.TryForceReleaseProxy(proxy.Id);
//                 }
//             }
//             await Task.Delay(_proxiesAvailableCheckPeriod, cancellationToken);
//         }
//     }
//
//     private bool TryCreateOrGetCachedGrpcChannel(string address, out GrpcChannel? createdChannel)
//     {
//         if (_cachedChannels.TryGetValue(address, out var channel))
//         {
//             createdChannel = channel;
//             return true;
//         }
//
//         var newChannel = GrpcChannel.ForAddress($"http://{address}");
//         if (!_cachedChannels.TryAdd(address, newChannel))
//         {
//             createdChannel = null;
//             return false;
//         }
//
//         createdChannel = newChannel;
//         return true;
//     }
//
//     private async Task<(bool heartbeatResult, Guid? releaseKey)> HeartbeatToTakerAsync(Proxy proxy)
//     {
//         (bool heartbeatResult, Guid? releaseKey) result = (false, null);
//
//         // TODO: validate node address by regex
//         if (
//             proxy.TakerAddress is null
//             || !TryCreateOrGetCachedGrpcChannel(proxy.TakerAddress, out var channel)
//         )
//         {
//             return (false, null);
//         }
//
//         var client = new ProxyHeartbeat.ProxyHeartbeatClient(channel);
//         try
//         {
//             var response = await client.HeartbeatAsync(new ProxyHeartbeatRequest());
//             if (
//                 response.ReleaseKey is not null
//                 && Guid.TryParse(response.ReleaseKey, out var parsedReleaseKey)
//                 && proxy.EqualsReleaseKey(parsedReleaseKey)
//             )
//             {
//                 result.heartbeatResult = true;
//                 result.releaseKey = parsedReleaseKey;
//             }
//         }
//         catch (RpcException) { }
//
//         return result;
//     }
// }
