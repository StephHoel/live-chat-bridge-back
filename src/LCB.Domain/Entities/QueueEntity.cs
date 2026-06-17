using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Enums;

namespace LCB.Domain.Entities;

[ExcludeFromCodeCoverage]
public class QueueEntity
{
    public QueueEntity()
    {
    }

    public QueueEntity(Guid? id, ProviderTypeEnum provider, string user, bool? selected, DateTime? joinedAt)
    {
        Id = id ?? Guid.NewGuid();
        Provider = provider;
        User = user;
        Selected = selected ?? false;
        JoinedAt = joinedAt ?? DateTime.UtcNow;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public ProviderTypeEnum Provider { get; set; }
    public string User { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}