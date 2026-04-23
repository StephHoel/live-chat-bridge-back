using LCB.Domain.Enums;

namespace LCB.Domain.Entities;

public class Queue(Guid? id, ProviderTypeEnum provider, string user, bool? selected, DateTime? joinedAt)
{
    public Guid Id { get; set; } = id ?? Guid.NewGuid();
    public ProviderTypeEnum Provider { get; set; } = provider;
    public string User { get; set; } = user;
    public bool Selected { get; set; } = selected ?? false;
    public DateTime JoinedAt { get; set; } = joinedAt ?? DateTime.UtcNow;
}