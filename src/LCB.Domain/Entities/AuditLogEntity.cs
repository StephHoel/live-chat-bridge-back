using LCB.Domain.Enums;
using LCB.Domain.Extensions;

namespace LCB.Domain.Entities;

public class AuditLogEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();
    public string ActorUser { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public AuditLogStatusEnum Status { get; private set; } = AuditLogStatusEnum.Info;
    public string? MetadataJson { get; private set; }

    public static AuditLogEntity Create(
        string actorUser,
        string action,
        string resource,
        AuditLogStatusEnum status,
        string? metadataJson = null,
        DateTime? createdAtUtc = null)
    {
        return new AuditLogEntity
        {
            ActorUser = actorUser,
            Action = action,
            Resource = resource,
            Status = status,
            MetadataJson = metadataJson,
            CreatedAtUtc = (createdAtUtc ?? DateTime.UtcNow).NormalizeToUtcMinus3()
        };
    }
}
