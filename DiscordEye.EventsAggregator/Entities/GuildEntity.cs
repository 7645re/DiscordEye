using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("Guild")]
public class GuildEntity
{
    [Key]
    public long Id { get; set; }
    
    public string IconUrl { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    public ICollection<UserEntity> UserEntities { get; set; } = new List<UserEntity>();
    
    public ICollection<ChannelEntity> Channels { get; set; } = new List<ChannelEntity>(); // Связь с ChannelEntity
}