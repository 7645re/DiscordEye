using Discord.WebSocket;
using DiscordEye.Shared.Events;

namespace DiscordEye.Node.Helpers;

public static class DiscordHelper
{
    public static UserVoiceChannelActionType DetermineEventType(
        SocketVoiceState channelStateBefore,
        SocketVoiceState channelStateAfter)
    {
        if (channelStateBefore.VoiceChannel == null 
            && channelStateAfter.VoiceChannel != null)
        {
            return UserVoiceChannelActionType.JoinedVoiceChannel;
        }
        
        if (channelStateBefore.VoiceChannel != null 
            && channelStateAfter.VoiceChannel == null)
        {
            return UserVoiceChannelActionType.LeftVoiceChannel;
        }
        
        if (channelStateBefore.IsSelfDeafened 
            && !channelStateAfter.IsSelfDeafened)
        {
            return UserVoiceChannelActionType.Undeafened;
        }
        
        if (!channelStateBefore.IsSelfDeafened 
            && channelStateAfter.IsSelfDeafened)
        {
            return UserVoiceChannelActionType.Deafened;
        }
        
        if (channelStateBefore.IsSelfMuted 
            && !channelStateAfter.IsSelfMuted)
        {
            return UserVoiceChannelActionType.Unmuted;
        }
        
        if (!channelStateBefore.IsSelfMuted 
            && channelStateAfter.IsSelfMuted)
        {
            return UserVoiceChannelActionType.Muted;
        }
        
        if (!channelStateBefore.IsVideoing 
            && channelStateAfter.IsVideoing)
        {
            return UserVoiceChannelActionType.VideoEnabled;
        }
        
        if (channelStateBefore.IsVideoing && !channelStateAfter.IsVideoing)
        {
            return UserVoiceChannelActionType.VideoDisabled;
        }
        
        if (channelStateBefore.IsStreaming && !channelStateAfter.IsStreaming)
        {
            return UserVoiceChannelActionType.StreamStopped;
        }

        if (!channelStateBefore.IsStreaming && channelStateAfter.IsStreaming)
        {
            return UserVoiceChannelActionType.StreamStarted;
        }

        throw new ArgumentException("Unknown event type");
    }
}