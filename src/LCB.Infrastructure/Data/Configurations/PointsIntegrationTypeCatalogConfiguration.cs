using LCB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCB.Infrastructure.Data.Configurations;

public class PointsIntegrationTypeCatalogConfiguration : IEntityTypeConfiguration<PointsIntegrationTypeCatalogEntity>
{
    public void Configure(EntityTypeBuilder<PointsIntegrationTypeCatalogEntity> builder)
    {
        builder.ToTable("PointsIntegrationTypeCatalog");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.StreamerUserId)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IntegrationType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Delta)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.HasIndex(x => new { x.StreamerUserId, x.Provider, x.IntegrationType })
            .IsUnique();
    }
}