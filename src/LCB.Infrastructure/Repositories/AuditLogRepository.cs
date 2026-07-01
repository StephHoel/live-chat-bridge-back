using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Extensions;
using LCB.Infrastructure.Data;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class AuditLogRepository(
    LcbDbContext context,
    ILogger<AuditLogRepository> logger)
    : RepositoryBase(logger), IAuditLogRepository
{
    public async Task<bool> CreateAsync(AuditLogEntity log)
        => await ExecuteAsync(async () =>
        {
            await context.AddAsync(log);
            return await context.SaveChangesAsync() > 0;
        }, nameof(CreateAsync));

    public async Task<IEnumerable<AuditLogEntity>> GetByPeriodAsync(DateTime startUtc, DateTime endUtc, string? actorUser = null)
        => await ExecuteAsync(async () =>
        {
            var start = startUtc.NormalizeToUtcMinus3();
            var end = endUtc.NormalizeToUtcMinus3();

            var query = context.Set<AuditLogEntity>()
                               .AsNoTracking()
                               .Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc <= end);

            if (!string.IsNullOrWhiteSpace(actorUser))
                query = query.Where(x => x.ActorUser == actorUser);

            return await query.OrderBy(x => x.CreatedAtUtc).ToListAsync();
        }, nameof(GetByPeriodAsync));
}
