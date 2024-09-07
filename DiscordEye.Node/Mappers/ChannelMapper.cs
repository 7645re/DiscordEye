using Discord.WebSocket;
using DiscordEye.DiscordListener;
using DiscordEye.Node.Dto;

namespace DiscordEye.Node.Mappers;

public static class ChannelMapper
{
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