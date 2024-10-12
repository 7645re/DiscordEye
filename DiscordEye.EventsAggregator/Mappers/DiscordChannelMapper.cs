using DiscordEye.EventsAggregator.Dto;

namespace DiscordEye.EventsAggregator.Mappers;

public static class DiscordChannelMapper
{
    public static DiscordChannel ToDiscordChannel(this DiscordChannelGrpc discordChannelGrpc,
        ulong guildId = default)
    {
        return new DiscordChannel
        {
            Id = discordChannelGrpc.Id,
            Name = discordChannelGrpc.Name,
            GuildId = guildId
        };
    }
}