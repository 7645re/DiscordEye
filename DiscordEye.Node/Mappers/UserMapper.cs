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

    public static DiscordUserResponse ToDiscordUserResponse(this DiscordUser discordUser)
    {
        return new DiscordUserResponse
        {
            Id = discordUser.Id,
            Username = discordUser.Username,
            Guilds =
            {
                discordUser.Guilds.Select(x => new DiscordGuildResponse
                {
                    Id = x.Id.ToString(),
                    IconUrl = x.IconUrl,
                    Name = x.Name,
                    OwnerId = x.OwnerId.ToString(),
                    MemberCount = x.MemberCount,
                    Channels = { x
                        .Channels
                        .Select(x => x.ToChannelResponse()) }
                })
            }
        };
    }
}