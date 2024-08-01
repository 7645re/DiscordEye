namespace DiscordEye.DiscordListenerNode;

public class KafkaOptions
{
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string MessageDeletedTopic { get; set; }
    public string StreamStartedTopic { get; set; }
    public string StreamStoppedTopic { get; set; }
    public string GetHost() => $"{Address}:{Port}";
}