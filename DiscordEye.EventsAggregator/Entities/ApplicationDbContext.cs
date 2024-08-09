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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>()
            .HasMany(u => u.GuildsEntities)
            .WithMany(g => g.UserEntities)
            .UsingEntity<Dictionary<string, object>>(
                "UserGuild",
                j => j.HasOne<GuildEntity>().WithMany().HasForeignKey("GuildId"),
                j => j.HasOne<UserEntity>().WithMany().HasForeignKey("UserId"));

        modelBuilder.Entity<ChannelEntity>()
            .HasOne(c => c.GuildEntity)
            .WithMany(g => g.Channels)
            .HasForeignKey(c => c.GuildId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MessageEntity>()
            .HasOne(m => m.ChannelEntity)
            .WithMany()
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MessageEntity>()
            .HasOne(m => m.GuildEntity)
            .WithMany()
            .HasForeignKey(m => m.GuildId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MessageEntity>()
            .HasOne(m => m.UserEntity)
            .WithMany(u => u.MessagesEntities)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
