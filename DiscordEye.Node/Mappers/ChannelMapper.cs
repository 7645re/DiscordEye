using Discord.WebSocket;
using DiscordEye.DiscordListener;
using DiscordEye.Node.Dto;

namespace DiscordEye.Node.Mappers;

public static class ChannelMapper
{
    public static DiscordChannelResponse ToChannelResponse(this DiscordChannel channel)
    {
        return new DiscordChannelResponse
        {
            Id = channel.Id.ToString(),
            Name = channel.Name,
            Type = channel.Type.ToChannelTypeResponse(),
        };
    }

    public static DiscordChannel ToChannel(this SocketGuildChannel socketGuildChannel)
    {
        return new DiscordChannel
        {
            Id = socketGuildChannel.Id,
            Name = socketGuildChannel.Name,
            Type = socketGuildChannel.RecognizeChannelType()
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

    public static DiscordChannelTypeResponse ToChannelTypeResponse(this DiscordChannelType type)
    {
        return type switch
        {
            DiscordChannelType.TextChannel => DiscordChannelTypeResponse.TextChannel,
            DiscordChannelType.VoiceChannel => DiscordChannelTypeResponse.VoiceChannel,
            DiscordChannelType.CategoryChannel => DiscordChannelTypeResponse.CategoryChannel,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}