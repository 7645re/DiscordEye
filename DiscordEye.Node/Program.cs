using DiscordEye.Infrastructure.Extensions;
using DiscordEye.Node.DiscordClientWrappers.EventClient;
using DiscordEye.Node.DiscordClientWrappers.RequestClient;
using DiscordEye.Node.Extensions;
using DiscordEye.Node.Options;
using DiscordEye.Node.Services;
using DiscordEye.Node.Services.Node;
using DiscordEye.Node.Services.ProxyHeartbeat;
using DiscordEye.Node.Services.ProxyHolder;
using DiscordEye.ProxyDistributor;
using DiscordEye.Shared.Events;
using DiscordEye.Shared.Options;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

var kafkaOptions = builder
    .Configuration
    .GetRequiredSection("Kafka")
    .Get<KafkaOptions>();

var proxyDistributorUrl = builder.Configuration.GetValue<string>("ProxyDistributorUrl");
if (proxyDistributorUrl is null)
    throw new ArgumentException($"ProxyDistributorUrl is null, check appsettings.json file");
builder.AddLogger();
builder.Services.AddCoreServices();
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetRequiredSection("Discord"));
builder.Services.AddGrpcClient<ProxyDistributorGrpcService.ProxyDistributorGrpcServiceClient>(opt =>
{
    opt.Address = new Uri(proxyDistributorUrl);
});
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingInMemory();
    
    x.AddRider(r =>
    {
        r.AddProducer<Guid, MessageDeletedEvent>(kafkaOptions.MessageDeletedTopic);
        r.AddProducer<Guid, MessageReceivedEvent>(kafkaOptions.MessageReceivedTopic);
        r.AddProducer<Guid, MessageUpdatedEvent>(kafkaOptions.MessageUpdatedTopic);
        r.AddProducer<Guid, UserBannedEvent>(kafkaOptions.UserBannedTopic);
        r.AddProducer<Guid, UserChangedAvatarEvent>(kafkaOptions.UserChangedAvatarTopic);
        r.AddProducer<Guid, UserGuildChangedNicknameEvent>(kafkaOptions.UserGuildChangedNicknameTopic);
        r.AddProducer<Guid, UserVoiceChannelActionEvent>(kafkaOptions.UserVoiceChannelActionTopic);

        r.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaOptions.GetHost());
        });
    });
});
builder.Services.AddGrpc();
builder.Services.AddSingleton<IProxyHolderService, ProxyHolderService>();
builder.Services.AddSingleton<IDiscordEventClient, DiscordEventClient>();
builder.Services.AddSingleton<IDiscordRequestClient, DiscordRequestClient>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var proxyHolderService = scope.ServiceProvider.GetRequiredService<IProxyHolderService>();
    var proxy = await proxyHolderService.ReserveProxyWithRetries(10);
    
    if (proxy is null)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError("Failed to reserve a proxy. The application will not start.");
        throw new NullReferenceException("Node can't work without a proxy");
    }
}

app.MapGrpcService<NodeService>();
app.MapGrpcService<ProxyHeartbeatService>();
app.Run();