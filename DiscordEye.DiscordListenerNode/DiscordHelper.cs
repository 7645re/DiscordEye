using Discord.WebSocket;
using DiscordEye.Shared.Events;

namespace DiscordEye.DiscordListenerNode;

public static class DiscordHelper
{
    public static DiscordEventType DetermineEventType(
        SocketVoiceState channelStateBefore,
        SocketVoiceState channelStateAfter)
    {
        if (channelStateBefore.VoiceChannel == null 
            && channelStateAfter.VoiceChannel != null)
        {
            return DiscordEventType.JoinedVoiceChannel;
        }
        
        if (channelStateBefore.VoiceChannel != null 
            && channelStateAfter.VoiceChannel == null)
        {
            return DiscordEventType.LeftVoiceChannel;
        }
        
        if (channelStateBefore.IsSelfDeafened 
            && !channelStateAfter.IsSelfDeafened)
        {
            return DiscordEventType.Undeafened;
        }
        
        if (!channelStateBefore.IsSelfDeafened 
            && channelStateAfter.IsSelfDeafened)
        {
            return DiscordEventType.Deafened;
        }
        
        if (channelStateBefore.IsSelfMuted 
            && !channelStateAfter.IsSelfMuted)
        {
            return DiscordEventType.Unmuted;
        }
        
        if (!channelStateBefore.IsSelfMuted 
            && channelStateAfter.IsSelfMuted)
        {
            return DiscordEventType.Muted;
        }
        
        if (!channelStateBefore.IsVideoing 
            && channelStateAfter.IsVideoing)
        {
            return DiscordEventType.VideoEnabled;
        }
        
        if (channelStateBefore.IsVideoing && !channelStateAfter.IsVideoing)
        {
            return DiscordEventType.VideoDisabled;
        }
        
        if (channelStateBefore.IsStreaming && !channelStateAfter.IsStreaming)
        {
            return DiscordEventType.StreamStopped;
        }

        if (!channelStateBefore.IsStreaming && channelStateAfter.IsStreaming)
        {
            return DiscordEventType.StreamStarted;
        }

        throw new ArgumentException("Unknown event type");
    }
}