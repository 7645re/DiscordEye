using DiscordEye.EventsAggregator;
using DiscordEye.EventsAggregator.Services.EventsAggregator;
using DiscordEye.EventsAggregator.Services.NodeCommunicateService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<INodeCommunicateService, NodeCommunicateService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddKafka(builder);
builder.Services.AddDatabase(builder
    .Configuration
    .GetConnectionString("DefaultConnection"));
builder.Services.AddMemoryCache();
builder.Services.AddGrpc();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<EventsAggregator>();
app.Run();