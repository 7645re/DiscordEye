namespace DiscordEye.DiscordListener.Dto;

public class DiscordGuild
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required string? IconUrl { get; set; }
    public required ulong OwnerId { get; set; }
    public required int MemberCount { get; set; }
    public required List<DiscordChannel> Channels { get; set; } = [];
}