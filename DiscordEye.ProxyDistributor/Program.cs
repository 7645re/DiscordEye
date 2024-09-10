using System.Net;
using DiscordEye.ProxyDistributor.BackgroundServices;
using DiscordEye.ProxyDistributor.Mappers;
using DiscordEye.ProxyDistributor.Services.ProxyDistributor;
using DiscordEye.ProxyDistributor.Services.ProxyStorage;
using DiscordEye.ProxyDistributor.Services.ProxyVault;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(opt =>
{
    opt.Listen(IPAddress.Any, 4999);
});
builder.Services.AddGrpc();
builder.Services.AddSingleton<IVaultClient>(_ =>
{
    var vaultAddress = "http://localhost:8200";
    var vaultToken = "root-token";
    return new VaultClient(new VaultClientSettings(vaultAddress, new TokenAuthMethodInfo(vaultToken)));
});
builder.Services.AddSingleton<IKeyValueSecretsEngineV2>(provider =>
{
    var vaultClient = provider.GetRequiredService<IVaultClient>();
    return vaultClient.V1.Secrets.KeyValue.V2;
});
builder.Services.AddSingleton<IProxyVaultService, ProxyVaultService>();

builder.Services.AddHostedService<ProxyHeartbeatBackgroundService>();


builder.Services.AddSingleton<IProxyStorageService>(provider =>
{
    var proxyVaultService = provider.GetRequiredService<IProxyVaultService>();
    var proxiesFromVault = proxyVaultService.GetAllProxiesAsync().GetAwaiter().GetResult();
    var proxies = proxiesFromVault.Select(x => x.ToProxy()).ToArray();
    var logger = provider.GetRequiredService<ILogger<ProxyStorageService>>();
    var serviceProvider = provider.GetRequiredService<IServiceProvider>();
    return new ProxyStorageService(proxies, logger, serviceProvider);
});

var app = builder.Build();

app.MapGrpcService<ProxyDistributorService>();
app.Run();