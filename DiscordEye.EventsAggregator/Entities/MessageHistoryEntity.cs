using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("MessageHistory")]
public class MessageHistoryEntity
{
    [Key]
    public long Id { get; set; }

    public ulong MessageId { get; set; }
    
    [MaxLength(4000)]
    public string Content { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }

    [ForeignKey("MessageId")]
    public MessageEntity MessageEntity { get; set; }
}