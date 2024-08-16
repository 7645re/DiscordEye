using Discord.WebSocket;
using DiscordEye.DiscordListener.Dto;
using DiscordEye.Shared.DiscordListenerApi.Response;

namespace DiscordEye.DiscordListener.Mappers;

public static class GuildMapper
{
    public static DiscordGuildResponse ToGuildResponse(this DiscordGuild discordGuild)
    {
        return new DiscordGuildResponse
        {
            Id = discordGuild.Id.ToString(),
            IconUrl = discordGuild.IconUrl,
            Name = discordGuild.Name,
            OwnerId = discordGuild.OwnerId.ToString(),
            MemberCount = discordGuild.MemberCount,
            Channels = discordGuild
                .Channels
                .Select(x => x.ToChannelResponse())
                .ToList()
        };
    }

    public static DiscordGuild ToDiscordGuild(
        this SocketGuild socketGuild,
        bool withChannels = false)
    {
        return new DiscordGuild
        {
            Id = socketGuild.Id,
            Name = socketGuild.Name,
            IconUrl = socketGuild.IconUrl,
            OwnerId = socketGuild.OwnerId,
            MemberCount = socketGuild.MemberCount,
            Channels = withChannels ? socketGuild
                .Channels
                .Select(x => x.ToChannel())
                .ToList() : []
        };
    }
}