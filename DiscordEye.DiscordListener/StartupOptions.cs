namespace DiscordEye.DiscordListener;

public class StartupOptions
{
    public string Token { get; set; }
    public int MessageCacheSize { get; set; }
    public bool SendEvents { get; set; }
}