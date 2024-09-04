using DiscordEye.ProxyDistributor.Options;
using DiscordEye.ProxyDistributor.Services;
using DiscordEye.ProxyDistributor.Services.ProxyDistributor;
using DiscordEye.ProxyDistributor.Services.ProxyStorage;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.Configure<ProxiesOptions>(builder.Configuration.GetSection("ProxiesSettings"));
builder.Services.AddSingleton<IProxyStorageService, ProxyStorageService>();
var app = builder.Build();
app.MapGrpcService<ProxyDistributorService>();
app.Run();