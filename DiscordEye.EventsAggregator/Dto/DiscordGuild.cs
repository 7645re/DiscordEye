namespace DiscordEye.EventsAggregator.Dto;

public class DiscordGuild
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required string? IconUrl { get; set; } = null;
    public required ulong OwnerId { get; set; }
    public required DiscordChannel[] Channels { get; set; } = [];
}