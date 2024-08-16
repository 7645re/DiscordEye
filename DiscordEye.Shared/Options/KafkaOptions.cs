namespace DiscordEye.Shared.Options;

public class KafkaOptions
{
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string MessageDeletedTopic { get; set; } = string.Empty;
    public string MessageReceivedTopic { get; set; } = string.Empty;
    public string MessageUpdatedTopic { get; set; } = string.Empty;
    public string UserBannedTopic { get; set; } = string.Empty;
    public string UserChangedAvatarTopic { get; set; } = string.Empty;
    public string UserGuildChangedNicknameTopic { get; set; } = string.Empty;
    public string UserVoiceChannelActionTopic { get; set; } = string.Empty;
    public string GetHost() => $"{Address}:{Port}";
}