using LCB.Domain.Enums;

namespace LCB.Domain.Models;

public record ChatMessageApiModel(
    Guid Id,
    string IdempotencyKey,
    ProviderTypeEnum Provider,
    string Author,
    string Text,
    DateTime Timestamp,
    bool Processed,
    DateTime CreatedAt,
    DateTime UpdatedAt
);