using DiscordEye.DiscordListener;
using DiscordEye.Node.Mappers;
using Grpc.Core;

namespace DiscordEye.Node.Services;

public class DiscordListenerService : DiscordListener.DiscordListener.DiscordListenerBase
{
    private readonly IServiceProvider _serviceProvider;

    public DiscordListenerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override Task<DiscordGuildResponse> GetGuild(GuildRequest request, ServerCallContext context)
    {
        var discordListener = _serviceProvider.GetHostedService<DiscordListenerBackgroundService>();
        var guild = discordListener.GetGuild(request.GuildId, request.WithChannels);
        return Task.FromResult(guild.ToGuildResponse());
    }
}