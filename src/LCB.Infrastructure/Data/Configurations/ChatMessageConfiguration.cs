using LCB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LCB.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessageEntity>
{
    public void Configure(EntityTypeBuilder<ChatMessageEntity> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Author)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.InsertedByUser)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Text)
            .HasMaxLength(2048);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Processed)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.HasIndex(x => x.InsertedByUser);

        builder.HasIndex(x => new { x.Processed, x.Timestamp });
    }
}
