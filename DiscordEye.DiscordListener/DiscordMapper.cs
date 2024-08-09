using Discord;
using Discord.API;
using Discord.Rest;
using DiscordEye.Shared.Response;

namespace DiscordEye.DiscordListener;

public static class DiscordMapper
{
    public static ChannelResponse ToChannelResponse(this IChannel channel)
    {
        return new ChannelResponse
        {
            Id = (long)channel.Id,
            Name = channel.Name
        };
    }
    
    public static UserResponse ToDiscordUserResponse(this UserProfile userProfile)
    {
        return new UserResponse
        {
            Id = (long)userProfile.User.Id,
            Username = userProfile.User.Username.Value,
            Guilds = userProfile
                .MutualGuilds
                .Select(x => new GuildResponse
                {
                    Id = (long)x.Id
                })
                .ToArray()
        };
    }

    public static GuildResponse ToGuildResponse(this RestGuild restGuild)
    {
        return new GuildResponse
        {
            Id = (long)restGuild.Id,
            IconUrl = restGuild.IconUrl,
            Name = restGuild.Name
        };
    }
}