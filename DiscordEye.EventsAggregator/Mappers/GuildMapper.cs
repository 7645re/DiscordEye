using DiscordEye.EventsAggregator.Dto;
using DiscordEye.Shared.NodeContracts.Response;

namespace DiscordEye.EventsAggregator.Mappers;

public static class GuildMapper
{
    public static Guild ToGuild(this DiscordGuildResponse discordGuildResponse)
    {
        return new Guild
        {
            Id = ulong.Parse(discordGuildResponse.Id),
            Name = discordGuildResponse.Name,
            IconUrl = discordGuildResponse.IconUrl
        };
    }
}