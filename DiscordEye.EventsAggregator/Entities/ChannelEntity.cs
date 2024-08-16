using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("Channel")]
public class ChannelEntity
{
    [Key]
    public ulong Id { get; set; }
    
    [MaxLength(100)]
    public string Name { get; set; }
    
    public ulong GuildId { get; set; }
    
    public GuildEntity GuildEntity { get; set; }
}