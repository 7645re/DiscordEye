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
    
    public List<GuildEntity> GuildsEntities { get; set; } = new();
    
    public List<MessageEntity> MessagesEntities { get; set; } = new();
}