namespace DiscordEye.Shared.Events;

public class KafkaOptions
{
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DiscordTopic { get; set; } = string.Empty;
    public string GetHost() => $"{Address}:{Port}";
}