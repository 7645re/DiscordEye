using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("MessageHistory")]
public class MessageHistoryEntity
{
    [Key]
    public long Id { get; set; }
    public long MessageId { get; set; }
    [MaxLength(4000)]
    public string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}