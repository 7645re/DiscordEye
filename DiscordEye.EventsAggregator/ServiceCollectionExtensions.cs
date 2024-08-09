using DiscordEye.EventsAggregator.Consumers;
using DiscordEye.EventsAggregator.Entities;
using DiscordEye.Shared;
using DiscordEye.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace DiscordEye.EventsAggregator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection serviceCollection,
        string? connectionString)
    {
        if (connectionString is null)
            throw new ArgumentException("ConnectionString is null");
        
        serviceCollection.AddDbContext<ApplicationDbContext>(opt =>
            opt.UseNpgsql(connectionString));
        return serviceCollection;
    }
    
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
                    r.AddConsumer<MessageDeletedEventConsumer>();
                    r.AddConsumer<MessageReceivedEventConsumer>();
                    r.AddConsumer<MessageUpdatedEventConsumer>();
                    r.AddConsumer<UserBannedEventConsumer>();
                    r.AddConsumer<UserChangedAvatarEventConsumer>();
                    r.AddConsumer<UserGuildChangedNicknameEventConsumer>();
                    r.AddConsumer<UserVoiceChannelActionEventConsumer>();
                    
                    r.UsingKafka((context, cfg) =>
                    {
                        cfg.TopicEndpoint<Guid, MessageDeletedEvent>(
                            kafkaOptions.MessageDeletedTopic,
                            "consumer-group-message-deleted", e =>
                            {
                                e.ConfigureConsumer<MessageDeletedEventConsumer>(context);
                            });

                        cfg.TopicEndpoint<Guid, MessageReceivedEvent>(
                            kafkaOptions.MessageReceivedTopic,
                            "consumer-group-message-received", e =>
                            {
                                e.ConfigureConsumer<MessageReceivedEventConsumer>(context);
                            });
                        
                        cfg.TopicEndpoint<Guid, MessageUpdatedEvent>(
                            kafkaOptions.MessageUpdatedTopic,
                            "consumer-group-message-updated", e =>
                            {
                                e.ConfigureConsumer<MessageUpdatedEventConsumer>(context);
                            });
                        
                        cfg.TopicEndpoint<Guid, UserBannedEvent>(
                            kafkaOptions.UserBannedTopic,
                            "consumer-group-user-banned", e =>
                            {
                                e.ConfigureConsumer<UserBannedEventConsumer>(context);
                            });
                        
                        cfg.TopicEndpoint<Guid, UserChangedAvatarEvent>(
                            kafkaOptions.UserChangedAvatarTopic,
                            "consumer-group-user-changed-avatar", e =>
                            {
                                e.ConfigureConsumer<UserChangedAvatarEventConsumer>(context);
                            });
               
                        cfg.TopicEndpoint<Guid, UserGuildChangedNicknameEvent>(
                            kafkaOptions.UserGuildChangedNicknameTopic,
                            "consumer-group-user-guild-changed-nickname", e =>
                            {
                                e.ConfigureConsumer<UserGuildChangedNicknameEventConsumer>(context);
                            });
                        
                        cfg.TopicEndpoint<Guid, UserVoiceChannelActionEvent>(
                            kafkaOptions.UserVoiceChannelActionTopic,
                            "consumer-group-user-voice-channel-action", e =>
                            {
                                e.ConfigureConsumer<UserVoiceChannelActionEventConsumer>(context);
                            });
                        
                        cfg.Host(kafkaOptions.GetHost());
                    });
                });
            });
    }
}