using LCB.Domain.Entities;
using LCB.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LCB.Infrastructure.Data;

public class LcbDbContext(DbContextOptions<LcbDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<QueueEntity> Queues => Set<QueueEntity>();
    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new QueueConfiguration());
        modelBuilder.ApplyConfiguration(new ChatMessageConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
