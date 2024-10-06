using Discord.Rest;
using DiscordEye.Node.Data;

namespace DiscordEye.Node.Mappers;

public static class ChannelMapper
{
    public static DiscordChannel ToChannel(this RestGuildChannel restGuildChannel)
    {
        return new DiscordChannel
        {
            Id = restGuildChannel.Id,
            Name = restGuildChannel.Name,
            Type = restGuildChannel.RecognizeChannelType()
        };
    }

    public static DiscordChannelGrpc ToDiscordChannelGrpc(this DiscordChannel channel)
    {
        return new DiscordChannelGrpc
        {
            Id = channel.Id,
            Name = channel.Name,
            Type = channel.RecognizeChannelType()
        };
    }
    
    private static DiscordChannelType RecognizeChannelType(this RestGuildChannel restGuildChannel)
    {
        return restGuildChannel switch
        {
            RestVoiceChannel => DiscordChannelType.VoiceChannel,
            RestTextChannel => DiscordChannelType.TextChannel,
            RestCategoryChannel => DiscordChannelType.CategoryChannel,
            RestForumChannel => DiscordChannelType.ForumChannel,
            _ => throw new ArgumentException($"Unknown discord channel type")
        };
    }
    
    private static DiscordChannelTypeGrpc RecognizeChannelType(this DiscordChannel discordChannel)
    {
        return discordChannel.Type switch
        {
            DiscordChannelType.TextChannel => DiscordChannelTypeGrpc.TextChannel,
            DiscordChannelType.VoiceChannel => DiscordChannelTypeGrpc.VoiceChannel,
            DiscordChannelType.CategoryChannel => DiscordChannelTypeGrpc.CategoryChannel,
            DiscordChannelType.ForumChannel => DiscordChannelTypeGrpc.ForumChannel,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}