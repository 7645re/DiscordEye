using Discord.API;
using DiscordEye.Node.Data;

namespace DiscordEye.Node.Mappers;

public static class UserMapper
{
    public static DiscordUser ToDiscordUser(
        this UserProfile userProfile)
    {
        return new DiscordUser
        {
            Id = userProfile.User.Id,
            Username = userProfile.User.Username.Value,
            Guilds = userProfile.MutualGuilds.Select(x => x.ToGuild()).ToList()
        };
    }

    public static DiscordUserGrpc ToDiscordUserGrpc(this DiscordUser discordUser)
    {
        return new DiscordUserGrpc
        {
            Id = discordUser.Id,
            Username = discordUser.Username,
            Guilds = { discordUser.Guilds.Select(x => x.ToDiscordGuildGrpc()) }
        };
    }
    
    private static DiscordGuild ToGuild(this MutualGuild mutualGuild)
    {
        return new DiscordGuild
        {
            Id = mutualGuild.Id,
            Name = string.Empty,
            IconUrl = string.Empty,
            OwnerId = 0,
            Channels = new List<DiscordChannel>(Array.Empty<DiscordChannel>())
        };
    }
}