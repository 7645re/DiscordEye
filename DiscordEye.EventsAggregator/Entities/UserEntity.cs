using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("User")]
public class UserEntity
{
    [Key]
    public ulong Id { get; set; }

    [MaxLength(32)]
    public string Username { get; set; }

    public ICollection<UserGuildEntity> UserGuilds { get; set; } = new List<UserGuildEntity>();

    public ICollection<MessageEntity> MessagesEntities { get; set; } = new List<MessageEntity>();
}