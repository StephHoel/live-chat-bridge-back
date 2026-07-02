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
    private const string LeaseTableName = "AuditTaskLeases";

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
                    (x.MetadataJson != null && x.MetadataJson.Contains(endpointCategory) && x.CreatedAtUtc < endpointCutoffUtc)
                    || (x.MetadataJson != null && x.MetadataJson.Contains(workerCategory) && x.CreatedAtUtc < workerCutoffUtc)
                    || (x.MetadataJson != null && x.MetadataJson.Contains(systemTaskCategory) && x.CreatedAtUtc < systemCutoffUtc)
                    || (x.MetadataJson == null && x.CreatedAtUtc < systemCutoffUtc))
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
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
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
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }, nameof(GetDatabaseSizeMbAsync));

    public async Task<bool> TryAcquireMaintenanceLeaseAsync(string leaseName, string ownerId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        => await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(leaseName) || string.IsNullOrWhiteSpace(ownerId))
                return false;

            var duration = leaseDuration <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : leaseDuration;
            var now = DateTime.UtcNow;
            var expiresAt = now.Add(duration);

            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = $@"
CREATE TABLE IF NOT EXISTS {LeaseTableName} (
    LeaseName TEXT NOT NULL PRIMARY KEY,
    OwnerId TEXT NOT NULL,
    ExpiresAtUtc TEXT NOT NULL,
    UpdatedAtUtc TEXT NOT NULL
);";
                await createTableCommand.ExecuteNonQueryAsync(cancellationToken);

                await using var command = connection.CreateCommand();
                command.CommandText = $@"
INSERT INTO {LeaseTableName} (LeaseName, OwnerId, ExpiresAtUtc, UpdatedAtUtc)
VALUES ($leaseName, $ownerId, $expiresAtUtc, $updatedAtUtc)
ON CONFLICT(LeaseName) DO UPDATE SET
    OwnerId = excluded.OwnerId,
    ExpiresAtUtc = excluded.ExpiresAtUtc,
    UpdatedAtUtc = excluded.UpdatedAtUtc
WHERE {LeaseTableName}.ExpiresAtUtc <= $nowUtc OR {LeaseTableName}.OwnerId = $ownerId;";

                var leaseNameParameter = command.CreateParameter();
                leaseNameParameter.ParameterName = "$leaseName";
                leaseNameParameter.Value = leaseName.Trim();
                command.Parameters.Add(leaseNameParameter);

                var ownerParameter = command.CreateParameter();
                ownerParameter.ParameterName = "$ownerId";
                ownerParameter.Value = ownerId.Trim();
                command.Parameters.Add(ownerParameter);

                var expiresAtParameter = command.CreateParameter();
                expiresAtParameter.ParameterName = "$expiresAtUtc";
                expiresAtParameter.Value = expiresAt.ToString("O");
                command.Parameters.Add(expiresAtParameter);

                var updatedAtParameter = command.CreateParameter();
                updatedAtParameter.ParameterName = "$updatedAtUtc";
                updatedAtParameter.Value = now.ToString("O");
                command.Parameters.Add(updatedAtParameter);

                var nowParameter = command.CreateParameter();
                nowParameter.ParameterName = "$nowUtc";
                nowParameter.Value = now.ToString("O");
                command.Parameters.Add(nowParameter);

                var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                return affectedRows > 0;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }, nameof(TryAcquireMaintenanceLeaseAsync));

    public async Task<bool> ReleaseMaintenanceLeaseAsync(string leaseName, string ownerId, CancellationToken cancellationToken = default)
        => await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(leaseName) || string.IsNullOrWhiteSpace(ownerId))
                return false;

            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync(cancellationToken);

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $@"
DELETE FROM {LeaseTableName}
WHERE LeaseName = $leaseName
  AND OwnerId = $ownerId;";

                var leaseNameParameter = command.CreateParameter();
                leaseNameParameter.ParameterName = "$leaseName";
                leaseNameParameter.Value = leaseName.Trim();
                command.Parameters.Add(leaseNameParameter);

                var ownerParameter = command.CreateParameter();
                ownerParameter.ParameterName = "$ownerId";
                ownerParameter.Value = ownerId.Trim();
                command.Parameters.Add(ownerParameter);

                var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
                return affectedRows > 0;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }, nameof(ReleaseMaintenanceLeaseAsync));
}
