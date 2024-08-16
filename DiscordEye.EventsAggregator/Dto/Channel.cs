namespace DiscordEye.EventsAggregator.Dto;

public class Channel
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required long GuildId { get; set; }
}