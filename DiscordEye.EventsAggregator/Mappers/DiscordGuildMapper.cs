using DiscordEye.EventsAggregator.Dto;

namespace DiscordEye.EventsAggregator.Mappers;

public static class DiscordGuildMapper
{
    public static DiscordGuild ToDiscordGuild(this DiscordGuildGrpc discordGuildGrpc)
    {
        return new DiscordGuild
        {
            Id = discordGuildGrpc.Id,
            Name = discordGuildGrpc.Name,
            IconUrl = discordGuildGrpc.IconUrl,
            OwnerId = discordGuildGrpc.OwnerId,
            Channels = discordGuildGrpc.Channels
                .Select(x => x.ToDiscordChannel())
                .ToArray()
        };
    }
}