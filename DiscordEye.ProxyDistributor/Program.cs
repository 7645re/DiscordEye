using DiscordEye.Infrastructure.Extensions;
using DiscordEye.ProxyDistributor.Extensions;
using DiscordEye.ProxyDistributor.Services.Heartbeat;
using DiscordEye.ProxyDistributor.Services.ProxyDistributor;
using DiscordEye.ProxyDistributor.Services.ProxyReservation;
using DiscordEye.ProxyDistributor.Services.SnapShoot;
using DiscordEye.ProxyDistributor.Services.Vault;

var builder = WebApplication.CreateBuilder(args);
builder.AddLogger();
builder.AddVault();
builder.Services.AddCoreServices();
builder.Services.AddGrpc();
builder.Services.AddSingleton<IProxyHeartbeatSnapShooter, ProxyHeartbeatSnapShooter>();
builder.Services.AddSingleton<IProxyStateSnapShooter, ProxyStateSnapShooter>();
builder.Services.AddSingleton<IProxyVaultService, ProxyVaultService>();
builder.Services.AddSingleton<IProxyDistributorService, ProxyDistributorService>();
builder.Services.AddSingleton<IProxyHeartbeatService, ProxyHeartbeatService>();
builder.Services.AddSingleton<IProxyReservationService, ProxyReservationService>();
builder.Services.AddQuartz();

var app = builder.Build();

app.MapGrpcService<ProxyDistributorGrpcService>();
app.Run();
