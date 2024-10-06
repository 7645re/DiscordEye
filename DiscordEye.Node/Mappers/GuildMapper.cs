using Discord.Rest;
using DiscordEye.Node.Data;

namespace DiscordEye.Node.Mappers;

public static class GuildMapper
{
    public static DiscordGuild ToDiscordGuild(this RestGuild restGuild, List<RestGuildChannel> restGuildChannels)
    {
        return new DiscordGuild
        {
            Id = restGuild.Id,
            Name = restGuild.Name,
            IconUrl = restGuild.IconUrl,
            OwnerId = restGuild.OwnerId,
            Channels = restGuildChannels
                .Select(x => x.ToChannel())
                .ToList()
        };
    }
    
    public static DiscordGuildGrpc ToDiscordGuildGrpc(this DiscordGuild discordGuild)
    {
        return new DiscordGuildGrpc
        {
            Id = discordGuild.Id.ToString(),
            IconUrl = discordGuild.IconUrl,
            Name = discordGuild.Name,
            OwnerId = discordGuild.OwnerId.ToString(),
            Channels = { discordGuild
                .Channels
                .Select(x => x.ToDiscordChannelGrpc())
                .ToList() }
        };
    }
}