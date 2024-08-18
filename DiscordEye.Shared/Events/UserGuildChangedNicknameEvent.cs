namespace DiscordEye.Shared.Events;

public class UserGuildChangedNicknameEvent
{
    public required int NodeId { get; set; }
    public required long GuildId { get; set; }
    public required long UserId { get; set; }
    public string? OldUsername { get; set; }
    public string? NewUsername { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}