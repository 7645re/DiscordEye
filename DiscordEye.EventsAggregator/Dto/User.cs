namespace DiscordEye.EventsAggregator.Dto;

public class User
{
    public required long Id { get; set; }
    public required string Username { get; set; }
    public required List<Guild> Guilds { get; set; }
}