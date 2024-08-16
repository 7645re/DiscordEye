using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordEye.EventsAggregator.Entities;

[Table("UserGuild")]
public class UserGuildEntity
{
    [Key]
    public long Id { get; set; }
    
    public ulong UserId { get; set; }
    public UserEntity User { get; set; }

    public ulong GuildId { get; set; }
    public GuildEntity Guild { get; set; }
}