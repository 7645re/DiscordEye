using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("MessageDeleted")]
public class MessageDeletedEntity
{
    [Key]
    public long Id { get; set; }
    
    public long ChannelId { get; set; }
    
    public long GuildId { get; set; }
    
    
}