using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("Channel")]
public class ChannelEntity
{
    [Key]
    public long Id { get; set; }
    
    [MaxLength(100)]
    public string Name { get; set; }
    
    public long GuildId { get; set; }
    
    public GuildEntity GuildEntity { get; set; }
}