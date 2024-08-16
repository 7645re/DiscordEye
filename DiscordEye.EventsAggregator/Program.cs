using DiscordEye.EventsAggregator;
using DiscordEye.EventsAggregator.Services.DiscordUserService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IDiscordUserService, DiscordUserService>();
builder.Services.AddTransient<IEventService, EventService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddKafka(builder);
builder.Services.AddDatabase(builder
    .Configuration
    .GetConnectionString("DefaultConnection"));
builder.Services.AddHttpClient<DiscordApiClient>();
builder.Services.AddMemoryCache();

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
app.Run();