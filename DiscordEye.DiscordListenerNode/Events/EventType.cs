namespace DiscordEye.DiscordListenerNode.Events;

public enum EventType
{
    MessageWritten,
    MessageDeleted,
    MessageChanged,
    StreamStarted,
    StreamStopped,
    UserJoinedVoiceChannel,
    UserLeftVoiceChannel,
    UserMuted,
    UserUnmuted,
    UserDeafened,
    UserUndeafened,
    UserVideoEnabled,
    UserVideoDisabled
}