using DiscordEye.DiscordListenerNode;
using DiscordEye.DiscordListenerNode.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var configuration = configurationBuilder.Build();
var kafkaOptions = configuration
    .GetRequiredSection("Kafka")
    .Get<KafkaOptions>();

var services = new ServiceCollection();
services.Configure<StartupOptions>(configuration
    .GetRequiredSection("Startup"));
services.Configure<KafkaOptions>(configuration
    .GetRequiredSection("Kafka"));

services.AddLogging(builder => builder.AddConsole());
services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.UsingInMemory();
    
    x.AddRider(r =>
    {
        r.AddProducer<Guid, MessageDeletedEvent>(kafkaOptions.MessageDeletedTopic);
        r.AddProducer<Guid, StreamStartedEvent>(kafkaOptions.StreamStartedTopic);
        r.AddProducer<Guid, StreamStoppedEvent>(kafkaOptions.StreamStoppedTopic);
                    
        r.UsingKafka((context, cfg) =>
        {
            cfg.Host(kafkaOptions.GetHost());
        });                            
    });
});

services.AddSingleton<DiscordListener>();
var serviceProvider = services.BuildServiceProvider();

var busControl = serviceProvider.GetRequiredService<IBusControl>();
await busControl.StartAsync();

var discordListener = serviceProvider.GetRequiredService<DiscordListener>();
await discordListener.StartAsync();

await Task.Delay(Timeout.Infinite);