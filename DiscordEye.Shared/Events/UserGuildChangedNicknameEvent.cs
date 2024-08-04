namespace DiscordEye.Shared.Events;

public class UserGuildChangedNicknameEvent
{
    public required ulong GuildId { get; set; }
    public required ulong UserId { get; set; }
    public string? OldUsername { get; set; }
    public string? NewUsername { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}