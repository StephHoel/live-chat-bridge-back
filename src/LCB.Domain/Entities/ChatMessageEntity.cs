using System.Diagnostics.CodeAnalysis;
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
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool Processed { get; set; } = false;

    public void EnsureIdempotencyKey()
    {
        if (!string.IsNullOrWhiteSpace(IdempotencyKey))
            return;

        IdempotencyKey = $"{Provider}:{Timestamp:yyyyMMddHHmmss}:{Id}";
    }
}