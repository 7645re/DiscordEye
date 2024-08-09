using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("User")]
public class UserEntity
{
    [Key]
    public long Id { get; set; }

    [MaxLength(32)]
    public string Username { get; set; }
    
    public ICollection<GuildEntity> GuildsEntities { get; set; } = new List<GuildEntity>();
    
    public ICollection<MessageEntity> MessagesEntities { get; set; } = new List<MessageEntity>();
}