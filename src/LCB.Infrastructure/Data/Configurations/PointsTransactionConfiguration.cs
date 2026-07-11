using LCB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCB.Infrastructure.Data.Configurations;

public class PointsTransactionConfiguration : IEntityTypeConfiguration<PointsTransactionEntity>
{
    public void Configure(EntityTypeBuilder<PointsTransactionEntity> builder)
    {
        builder.ToTable("PointsTransactions");

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
            .IsRequired();

        builder.Property(x => x.Situation)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.TransactionDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.Provider, x.ChannelId, x.UserId });
        builder.HasIndex(x => x.TransactionDateTime);
    }
}
