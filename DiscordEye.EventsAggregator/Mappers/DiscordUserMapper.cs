using DiscordEye.EventsAggregator.Dto;

namespace DiscordEye.EventsAggregator.Mappers;

public static class DiscordUserMapper
{
    public static DiscordUser ToDiscordUser(this DiscordUserGrpc discordUserGrpc)
    {
        return new DiscordUser
        {
            Id = discordUserGrpc.Id,
            Username = discordUserGrpc.Username,
            Guilds = discordUserGrpc.Guilds.Select(x => x.ToDiscordGuild()).ToArray()
        };
    }
}