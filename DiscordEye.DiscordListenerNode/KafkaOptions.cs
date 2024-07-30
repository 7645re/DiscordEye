namespace DiscordEye.DiscordListenerNode;

public class KafkaOptions
{
    public string Address { get; set; } = string.Empty;
    
    public int Port { get; set; }
    
    public string MessageDeleteTopic { get; set; }

    public string StreamPreviewTopic { get; set; }
    
    public string GetHost() => $"{Address}:{Port}";
}