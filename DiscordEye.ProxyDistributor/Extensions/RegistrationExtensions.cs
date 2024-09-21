using DiscordEye.ProxyDistributor.Jobs;
using Quartz;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

namespace DiscordEye.ProxyDistributor.Extensions;

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

    public static WebApplicationBuilder AddVault(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IVaultClient>(_ =>
        {
            var vaultOpt = builder.Configuration.GetSection("Vault");
            var address = vaultOpt.GetValue<string>("Address");
            var token = vaultOpt.GetValue<string>("Token");
            return new VaultClient(new VaultClientSettings(address, new TokenAuthMethodInfo(token)));
        });
        builder.Services.AddSingleton<IKeyValueSecretsEngineV2>(provider =>
        {
            var vaultClient = provider.GetRequiredService<IVaultClient>();
            return vaultClient.V1.Secrets.KeyValue.V2;
        });
        return builder;
    }

    public static IServiceCollection AddQuartz(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddQuartz(q =>
        {
            var jobKey = new JobKey("ProxiesHeartbeatsJob");
            q.AddJob<ProxiesHeartbeatsJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ProxiesHeartbeatsJob-trigger")
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10)
                    .RepeatForever()));
        });
        serviceCollection.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        return serviceCollection;
    }
}