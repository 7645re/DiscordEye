using Discord.API;
using DiscordEye.DiscordListener;
using DiscordEye.Node.Dto;

namespace DiscordEye.Node.Mappers;

public static class UserMapper
{
    public static DiscordUser ToDiscordUser(
        this UserProfile userProfile,
        List<DiscordGuild>? guilds = null)
    {
        return new DiscordUser
        {
            Id = userProfile.User.Id,
            Username = userProfile.User.Username.Value,
            Guilds = guilds ?? []
        };
    }

    public static DiscordUserGrpc ToDiscordUserGrpc(this DiscordUser discordUser)
    {
        return new DiscordUserGrpc
        {
            Id = discordUser.Id,
            Username = discordUser.Username
        };
    }
}