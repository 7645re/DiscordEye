namespace DiscordEye.Shared.Events;

public class DiscordEvent
{
    public DiscordEventType EventType { get; set; }
    public string ContentJson { get; set; } = string.Empty;
}