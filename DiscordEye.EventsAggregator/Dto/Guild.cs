namespace DiscordEye.EventsAggregator.Dto;

public class Guild
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required string? IconUrl { get; set; } = null;
}