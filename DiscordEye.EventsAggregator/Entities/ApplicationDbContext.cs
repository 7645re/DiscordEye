using Microsoft.EntityFrameworkCore;

namespace DiscordEye.EventsAggregator.Entities;

public class ApplicationDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<GuildEntity> Guilds { get; set; }
    public DbSet<ChannelEntity> Channels { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<MessageHistoryEntity> MessageHistories { get; set; }
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
}