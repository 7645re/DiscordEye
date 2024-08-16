namespace DiscordEye.Shared.Events;

public class UserVoiceChannelActionEvent
{
    public required ulong GuildId { get; set; }
    public required ulong ChannelId { get; set; }
    public required ulong UserId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required UserVoiceChannelActionType ActionType { get; set; }
    public string? Attachment { get; set; }
}