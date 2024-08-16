using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("Guild")]
public class GuildEntity
{
    [Key]
    public ulong Id { get; set; }
    
    public string? IconUrl { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    public ICollection<UserGuildEntity> UserGuilds { get; set; } = new List<UserGuildEntity>();
    
    public ICollection<ChannelEntity> Channels { get; set; } = new List<ChannelEntity>();
}