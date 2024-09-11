using DiscordEye.Infrastructure.Extensions;
using DiscordEye.Infrastructure.Services.Lock;
using DiscordEye.ProxyDistributor.BackgroundServices;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.Node.Manager;
using DiscordEye.ProxyDistributor.Services.ProxyDistributor;
using DiscordEye.ProxyDistributor.Services.ProxyStorage;
using DiscordEye.ProxyDistributor.Services.ProxyVault;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoreServices();

builder.Services.AddGrpc();
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
builder.Services.AddSingleton<IProxyVaultService, ProxyVaultService>();
builder.Services.AddSingleton<ITakerNodesManager, TakerNodesManager>();

builder.Services.AddHostedService<ProxyHeartbeatBackgroundService>();

builder.Services.AddSingleton<IProxyStorageService>(provider =>
{
    var proxyVaultService = provider.GetRequiredService<IProxyVaultService>();
    var proxiesFromVault = proxyVaultService.GetAllProxiesAsync().GetAwaiter().GetResult();
    var proxies = proxiesFromVault.Select(x => x.ToProxy()).ToArray();
    var logger = provider.GetRequiredService<ILogger<ProxyStorageService>>();
    var serviceProvider = provider.GetRequiredService<IServiceProvider>();
    var locker = provider.GetRequiredService<KeyedLockService>();
    var takerNodesManager = provider.GetRequiredService<ITakerNodesManager>();

    return new ProxyStorageService(proxies, logger, serviceProvider, takerNodesManager, locker);
});

var app = builder.Build();

app.MapGrpcService<ProxyDistributorService>();
app.Run();
