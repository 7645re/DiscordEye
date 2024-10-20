using DiscordEye.TokenDistributor.Services.TokenDistributor;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<TokenDistributorGrpcService>();
app.Run();