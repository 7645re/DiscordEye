using DiscordEye.Node.Services.ProxyHolder;
using DiscordEye.Shared.Extensions;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace DiscordEye.Node.Extensions;

public static class RegistrationExtensions
{
    public static WebApplicationBuilder AddLogger(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
                new Uri(builder.Configuration["ElasticConfiguration:Uri"]))
            {
                AutoRegisterTemplate = true,
                IndexFormat = "logstash-{0:yyyy.MM.dd}"
            })
            .CreateLogger();
        builder.Host.UseSerilog();
        return builder;
    }

    public static WebApplicationBuilder AddGrpcClients(this WebApplicationBuilder builder)
    {
        RegisterProxyDistributorGpcClient();
        RegisterEventsAggregatorGrpcClient();

        void RegisterProxyDistributorGpcClient()
        {
            var proxyDistributorUrl = builder.Configuration.GetValue<string>("ProxyDistributorUrl");
            if (proxyDistributorUrl is null)
                throw new ArgumentException($"ProxyDistributorUrl is null, check appsettings.json file");

            builder.Services.AddGrpcClient<ProxyDistributorGrpc.ProxyDistributorGrpcClient>(opt =>
            {
                opt.Address = new Uri(proxyDistributorUrl);
            });
        }

        void RegisterEventsAggregatorGrpcClient()
        {
            var aggregatorUrl = builder.Configuration.GetValue<string>("EventsAggregatorUrl");
            if (aggregatorUrl is null)
                throw new ArgumentException($"EventsAggregatorUrl is null, check appsettings.json file");
            
            builder.Services.AddGrpcClient<EventsAggregatorGrpc.EventsAggregatorGrpcClient>(opt =>
            {
                opt.Address = new Uri(aggregatorUrl);
            });
        }
        
        return builder;
    }
    
    public static async Task RegisterNodeToSystem(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await ReserveProxyForInit();
        await RegisterNodeToEventsAggregator();

        async Task ReserveProxyForInit()
        {
            var proxyHolderService = scope.ServiceProvider.GetRequiredService<IProxyHolderService>();
            var proxy = await proxyHolderService.ReserveProxyWithRetries(10);
    
            if (proxy is null)
            {
                logger.LogError("Failed to reserve a proxy. The application will not start.");
                throw new NullReferenceException("Node can't work without a proxy");
            }
        }

        async Task RegisterNodeToEventsAggregator()
        {
            var eventsAggregatorGrpcClient = scope.ServiceProvider
                .GetRequiredService<EventsAggregatorGrpc.EventsAggregatorGrpcClient>();
            var nodeAddress = $"localhost:{StartupExtensions.GetPort()}";
            var registerResult = await eventsAggregatorGrpcClient
                .RegisterNodeAsync(new RegisterRequest
                {
                    NodeAddress = nodeAddress
                });

            if (registerResult.Result == RegisterResult.Fail)
            {
                throw new InvalidOperationException("Failed to register node in events aggregator");
            }

            logger.LogInformation("Node is successfully registered in the events aggregator");
        }
    }
}