using LCB.Domain.Entities;
using LCB.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LCB.Infrastructure.Data;

public class LcbDbContext(DbContextOptions<LcbDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<QueueEntity> Queues => Set<QueueEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();
    public DbSet<LiveSettingsEntity> LiveSettings => Set<LiveSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new QueueConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());
        modelBuilder.ApplyConfiguration(new LiveSettingsConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
