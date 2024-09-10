using System.Net;
using DiscordEye.Node.BackgroundServices;
using DiscordEye.Node.DiscordClientWrappers.EventClient;
using DiscordEye.Node.DiscordClientWrappers.RequestClient;
using DiscordEye.Node.Options;
using DiscordEye.Node.Services;
using DiscordEye.ProxyDistributor;
using DiscordEye.Shared.Events;
using DiscordEye.Shared.Options;
using Grpc.Net.Client;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(opt =>
{
    opt.Listen(IPAddress.Any, 5000);
});
var kafkaOptions = builder
    .Configuration
    .GetRequiredSection("Kafka")
    .Get<KafkaOptions>();

var proxyDistributorUrl = builder.Configuration.GetValue<string>("ProxyDistributorUrl");
if (proxyDistributorUrl is null)
    throw new ArgumentException($"ProxyDistributorUrl is null, check appsettings.json file");
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetRequiredSection("Discord"));
builder.Services.AddGrpcClient<ProxyDistributorService.ProxyDistributorServiceClient>(opt =>
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
builder.Services.AddHostedService<DiscordFacadeBackgroundService>();
builder.Services.AddSingleton<IDiscordEventClient, DiscordEventClient>();
builder.Services.AddSingleton<IDiscordRequestClient, DiscordRequestClient>();
var app = builder.Build();
app.MapGrpcService<DiscordListenerService>();
app.MapGrpcService<ProxyHeartbeatService>();
app.Run();