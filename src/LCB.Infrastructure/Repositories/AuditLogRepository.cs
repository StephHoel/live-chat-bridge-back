using LCB.Domain.Constants;
using LCB.Domain.Entities;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Repositories;
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

    public async Task<int> PurgeExpiredAsync(DateTime endpointCutoffUtc, DateTime workerCutoffUtc, DateTime systemCutoffUtc, int batchSize)
        => await ExecuteAsync(async () =>
        {
            if (batchSize <= 0)
                return 0;

            var endpointCategory = $"\"eventCategory\":\"{AuditLogCatalog.EventCategory.EndpointOperational}\"";
            var workerCategory = $"\"eventCategory\":\"{AuditLogCatalog.EventCategory.WorkerFlow}\"";
            var systemTaskCategory = $"\"eventCategory\":\"{AuditLogCatalog.EventCategory.SystemTask}\"";

            var candidates = await context.Set<AuditLogEntity>()
                .Where(x =>
                    (x.MetadataJson != null && x.MetadataJson.Contains(endpointCategory) && x.CreatedAtUtc < endpointCutoffUtc) ||
                    (x.MetadataJson != null && x.MetadataJson.Contains(workerCategory) && x.CreatedAtUtc < workerCutoffUtc) ||
                    (x.MetadataJson != null && x.MetadataJson.Contains(systemTaskCategory) && x.CreatedAtUtc < systemCutoffUtc))
                .OrderBy(x => x.CreatedAtUtc)
                .Take(batchSize)
                .ToListAsync();

            if (candidates.Count == 0)
                return 0;

            context.RemoveRange(candidates);
            await context.SaveChangesAsync();

            return candidates.Count;
        }, nameof(PurgeExpiredAsync));

    public async Task<int> CountAsync()
        => await ExecuteAsync(async () => await context.Set<AuditLogEntity>().CountAsync(), nameof(CountAsync));

    public async Task<double> GetDatabaseSizeMbAsync()
        => await ExecuteAsync(async () =>
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var pageCountCommand = connection.CreateCommand();
            pageCountCommand.CommandText = "PRAGMA page_count;";

            await using var pageSizeCommand = connection.CreateCommand();
            pageSizeCommand.CommandText = "PRAGMA page_size;";

            var pageCountRaw = await pageCountCommand.ExecuteScalarAsync();
            var pageSizeRaw = await pageSizeCommand.ExecuteScalarAsync();

            var pageCount = Convert.ToDouble(pageCountRaw ?? 0d);
            var pageSize = Convert.ToDouble(pageSizeRaw ?? 0d);

            var bytes = pageCount * pageSize;
            return bytes / (1024d * 1024d);
        }, nameof(GetDatabaseSizeMbAsync));
}
