using LCB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCB.Infrastructure.Data.Configurations;

public class PointsBalanceConfiguration : IEntityTypeConfiguration<PointsBalanceEntity>
{
    public void Configure(EntityTypeBuilder<PointsBalanceEntity> builder)
    {
        builder.ToTable("PointsBalances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ChannelId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Points)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Único saldo ativo por combinação provider + channelId + userId
        builder.HasIndex(x => new { x.Provider, x.ChannelId, x.UserId, x.IsActive })
            .IsUnique()
            .HasFilter("\"IsActive\" = 1");

        builder.HasIndex(x => new { x.Provider, x.ChannelId, x.UserId });
    }
}
