using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Enums;
using LCB.Domain.Extensions;

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
        JoinedAt = (joinedAt ?? DateTimeExtensions.NowUtcMinus3()).NormalizeToUtcMinus3();
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public ProviderTypeEnum Provider { get; set; }
    public string User { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public DateTime JoinedAt { get; set; } = DateTimeExtensions.NowUtcMinus3();
    public DateTime CreatedAt { get; set; } = DateTimeExtensions.NowUtcMinus3();
    public DateTime UpdatedAt { get; set; } = DateTimeExtensions.NowUtcMinus3();

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTimeExtensions.NowUtcMinus3();
    }
}