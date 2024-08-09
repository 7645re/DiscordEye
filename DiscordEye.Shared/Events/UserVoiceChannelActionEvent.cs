namespace DiscordEye.Shared.Events;

public class UserVoiceChannelActionEvent
{
    public required long GuildId { get; set; }
    public required long ChannelId { get; set; }
    public required long UserId { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required UserVoiceChannelActionType ActionType { get; set; }
    public string? Attachment { get; set; }
}