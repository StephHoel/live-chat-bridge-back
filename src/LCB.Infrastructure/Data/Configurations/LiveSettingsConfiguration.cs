using LCB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCB.Infrastructure.Data.Configurations;

public class LiveSettingsConfiguration : IEntityTypeConfiguration<LiveSettingsEntity>
{
    public void Configure(EntityTypeBuilder<LiveSettingsEntity> builder)
    {
        builder.ToTable("LiveSettings");

        builder.HasKey(x => x.SettingsId);

        builder.Property(x => x.SettingsId)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.TikTokUsername)
            .HasMaxLength(255);

        builder.Property(x => x.TwitchUsername)
            .HasMaxLength(255);

        builder.Property(x => x.YouTubeUsername)
            .HasMaxLength(255);

        builder.Property(x => x.ReloadTimeInSec)
            .IsRequired()
            .HasDefaultValue(5L);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedByUser)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.HasIndex(x => x.UpdatedAtUtc);

        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
