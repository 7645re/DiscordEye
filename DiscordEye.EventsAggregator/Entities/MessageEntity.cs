using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("Message")]
public class MessageEntity
{
    [Key]
    public long Id { get; set; }
    public long GuildId { get; set; }
    public GuildEntity GuildEntity { get; set; }
    public long ChannelId { get; set; }
    public ChannelEntity ChannelEntity { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<MessageHistoryEntity> MessageHistoryEntities { get; set; } 
}