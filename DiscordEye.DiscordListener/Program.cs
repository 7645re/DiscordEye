using DiscordEye.DiscordListener;
using DiscordEye.Shared;
using DiscordEye.Shared.Events;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

var kafkaOptions = builder
    .Configuration
    .GetRequiredSection("Kafka")
    .Get<KafkaOptions>();

builder.Services.AddControllers();
builder.Services.Configure<StartupOptions>(builder.Configuration.GetRequiredSection("Startup"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
builder.Services.AddHostedService<DiscordListenerBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();