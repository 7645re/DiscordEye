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

    public static DiscordUserGrpcResponse ToDiscordUserGrpcResponse(this DiscordUser discordUser)
    {
        return new DiscordUserGrpcResponse
        {
            Id = discordUser.Id,
            Username = discordUser.Username,
            Guilds = 
            {
                discordUser.Guilds.Select(guild => new DiscordGuildGrpcResponse
                {
                    Id = guild.Id.ToString(),
                    IconUrl = guild.IconUrl,
                    Name = guild.Name,
                    OwnerId = guild.OwnerId.ToString(),
                    MemberCount = guild.MemberCount,
                    Channels = { guild.Channels.Select(channel => channel.ToChannelResponse()).ToList() }
                }).ToList()
            }
        };
    }
}