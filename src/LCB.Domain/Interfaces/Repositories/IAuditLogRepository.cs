using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task<bool> CreateAsync(AuditLogEntity log);
    Task<IEnumerable<AuditLogEntity>> GetByPeriodAsync(DateTime startUtc, DateTime endUtc, string? actorUser = null);
}
