using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Services;

public interface IAuditLogService
{
    Task<bool> WriteAsync(
        string actorUser,
        string action,
        string resource,
        AuditLogStatusEnum status,
        string? metadataJson = null,
        DateTime? createdAtUtc = null);

    Task<bool> WriteWithPolicyAsync(
        string actorUser,
        string action,
        string resource,
        AuditLogStatusEnum status,
        string? metadataJson = null,
        DateTime? createdAtUtc = null);
}
