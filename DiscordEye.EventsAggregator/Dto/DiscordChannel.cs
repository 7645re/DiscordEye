namespace DiscordEye.EventsAggregator.Dto;

public class DiscordChannel
{
    public required ulong Id { get; set; }
    public required string Name { get; set; }
    public required ulong GuildId { get; set; }
}