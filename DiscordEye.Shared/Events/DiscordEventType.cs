namespace DiscordEye.Shared.Events;

public enum DiscordEventType
{
    MessageWritten,
    MessageDeleted,
    MessageChanged,
    StreamStarted,
    StreamStopped,
    JoinedVoiceChannel,
    LeftVoiceChannel,
    Muted,
    Unmuted,
    Deafened,
    Undeafened,
    VideoEnabled,
    VideoDisabled,
    Banned,
    UserGuildChangedNickname,
    UserChangedAvatar
}