using DiscordEye.EventsAggregator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IEventService, EventService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddKafka(builder);
builder.Services.AddDatabase(builder
    .Configuration
    .GetConnectionString("DefaultConnection"));
builder.Services.AddHttpClient<DiscordApiClient>();

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