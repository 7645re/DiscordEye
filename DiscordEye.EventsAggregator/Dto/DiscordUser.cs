namespace DiscordEye.EventsAggregator.Dto;

public class DiscordUser
{
    public required ulong Id { get; set; }
    public required string Username { get; set; }
    public required DiscordGuild[] Guilds { get; set; } = [];
}