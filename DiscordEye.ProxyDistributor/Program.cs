using DiscordEye.Infrastructure.Extensions;
using DiscordEye.ProxyDistributor.Services.Heartbeat;
using DiscordEye.ProxyDistributor.Services.ProxyDistributor;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using DiscordEye.ProxyDistributor.Services.ProxyStateSnapShooter;
using DiscordEye.ProxyDistributor.Services.Vault;
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
builder.Services.AddSingleton<IProxyStateSnapShooter, ProxyStateSnapShooter>();
builder.Services.AddSingleton<IProxyVaultService, ProxyVaultService>();
builder.Services.AddSingleton<IProxyDistributorService, ProxyDistributorService>();
builder.Services.AddSingleton<IProxyHeartbeatService, ProxyHeartbeatService>();
builder.Services.AddSingleton<IProxyReservationService, ProxyReservationService>();

var app = builder.Build();

app.MapGrpcService<ProxyDistributorGrpcService>();
app.Run();
