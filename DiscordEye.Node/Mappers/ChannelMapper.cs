using Discord.WebSocket;
using DiscordEye.DiscordListener;
using DiscordEye.Node.Data;

namespace DiscordEye.Node.Mappers;

public static class ChannelMapper
{
    public static DiscordChannel ToChannel(this SocketGuildChannel socketGuildChannel)
    {
        return new DiscordChannel
        {
            Id = socketGuildChannel.Id,
            Name = socketGuildChannel.Name,
            Type = socketGuildChannel.RecognizeChannelType()
        };
    }

    public static DiscordChannelGrpc ToDiscordChannelGrpc(this DiscordChannel channel)
    {
        return new DiscordChannelGrpc
        {
            Id = channel.Id.ToString(),
            Name = channel.Name,
            Type = channel.RecognizeChannelType()
        };
    }
    
    private static DiscordChannelType RecognizeChannelType(this SocketGuildChannel socketGuildChannel)
    {
        return socketGuildChannel switch
        {
            SocketVoiceChannel => DiscordChannelType.VoiceChannel,
            SocketTextChannel => DiscordChannelType.TextChannel,
            SocketCategoryChannel => DiscordChannelType.CategoryChannel,
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
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}