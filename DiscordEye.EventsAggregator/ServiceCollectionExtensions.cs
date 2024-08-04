using DiscordEye.Shared.Events;
using MassTransit;

namespace DiscordEye.EventsAggregator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection serviceCollection,
        WebApplicationBuilder builder)
    {
        var kafkaOptions = builder
            .Configuration
            .GetSection("Kafka")
            .Get<KafkaOptions>()!;

        return serviceCollection
            .AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.UsingInMemory();
                
                x.AddRider(r =>
                {
                    r.AddConsumer<DiscordEventsConsumer>();
                    
                    r.UsingKafka((context, cfg) =>
                    {
                        cfg.TopicEndpoint<Guid, DiscordEvent>(
                            kafkaOptions.DiscordTopic,
                            "consumer-group-1", e =>
                            {
                                e.ConfigureConsumer<DiscordEventsConsumer>(context);
                            });
                        
                        cfg.Host(kafkaOptions.GetHost());
                    });
                });
            });
    }
}