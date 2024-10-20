using DiscordEye.Infrastructure.Extensions;
using DiscordEye.Node.Extensions;
using DiscordEye.Node.Options;
using DiscordEye.Node.Services.DiscordClient;
using DiscordEye.Node.Services.Node;
using DiscordEye.Node.Services.ProxyHeartbeat;
using DiscordEye.Node.Services.ProxyHolder;
using DiscordEye.Shared.Events;
using DiscordEye.Shared.Options;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
builder.AddLogger();
builder.Services.AddCoreServices();
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetRequiredSection("Discord"));
builder.AddGrpcClients();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingInMemory();
    
    x.AddRider(r =>
    {
        var kafkaOptions = builder
            .Configuration
            .GetRequiredSection("Kafka")
            .Get<KafkaOptions>();
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
builder.Services.AddSingleton<IDiscordClientService, DiscordClientService>();
var app = builder.Build();
await app.RegisterNodeToSystem();
app.ActivateDiscordClientService();

app.MapGrpcService<NodeService>();
app.MapGrpcService<ProxyHeartbeatService>();
app.Run();