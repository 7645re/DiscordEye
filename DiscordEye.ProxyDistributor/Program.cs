using DiscordEye.ProxyDistributor.BackgroundServices;
using DiscordEye.ProxyDistributor.Services.ProxyDistributor;
using DiscordEye.ProxyDistributor.Services.ProxyStorage;
using DiscordEye.ProxyDistributor.Services.ProxyVault;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddSingleton<IVaultClient>(provider =>
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
builder.Services.AddSingleton<IProxyStorageService, ProxyStorageService>();
builder.Services.AddHostedService<ProxyHeartbeatBackgroundService>();
var app = builder.Build();
app.MapGrpcService<ProxyDistributorService>();
app.Run();