using LCB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCB.Infrastructure.Data.Configurations;

public class QueueConfiguration : IEntityTypeConfiguration<QueueEntity>
{
    public void Configure(EntityTypeBuilder<QueueEntity> builder)
    {
        builder.ToTable("Queues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.User)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Selected)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.User)
            .IsUnique();
    }
}
