using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using LCB.Domain.Enums;

namespace LCB.Domain.Entities;

[ExcludeFromCodeCoverage]
public class ChatMessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdempotencyKey { get; set; } = string.Empty;
    public ProviderTypeEnum Provider { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void EnsureIdempotencyKey()
    {
        if (!string.IsNullOrWhiteSpace(IdempotencyKey))
            return;

        Author = Author.Trim();
        Timestamp = Timestamp.Kind == DateTimeKind.Utc ? Timestamp : Timestamp.ToUniversalTime();
        IdempotencyKey = $"{Provider}:{Author}:{Timestamp.ToString("O", CultureInfo.InvariantCulture)}";
    }

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}