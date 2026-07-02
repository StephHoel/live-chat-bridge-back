using LCB.Application.Helpers;
using LCB.Domain.Constants;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LCB.Application.Workers;

public class AuditRetentionCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AuditRetentionPolicy> retentionOptions,
    ILogger<AuditRetentionCleanupWorker> logger) : BackgroundService
{
    private const string TaskName = "AuditRetentionCleanup";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Max(1, retentionOptions.Value.CleanupIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteCycleAsync(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExecuteCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        var executionId = Guid.NewGuid().ToString("N");

        await auditLogService.WriteWithPolicyAsync(
            "system:worker",
            AuditLogCatalog.Action.SystemTaskStarted,
            AuditLogCatalog.Resource.SystemTask,
            AuditLogStatusEnum.Info,
            AuditMetadataFactory.CreateSystemTask(TaskName, executionId, "Started"));

        try
        {
            var now = DateTime.UtcNow;
            var options = retentionOptions.Value;

            var endpointCutoff = now.AddDays(-Math.Max(1, options.EndpointOperationalTtlDays));
            var workerCutoff = now.AddDays(-Math.Max(1, options.WorkerFlowTtlDays));
            var systemCutoff = now.AddDays(-Math.Max(1, options.SystemTaskTtlDays));
            var batchSize = Math.Max(1, options.BatchSize);

            var totalPurged = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var purged = await repository.PurgeExpiredAsync(endpointCutoff, workerCutoff, systemCutoff, batchSize);
                totalPurged += purged;

                if (purged == 0)
                    break;
            }

            var totalRows = await repository.CountAsync();
            var totalMb = await repository.GetDatabaseSizeMbAsync();

            if (totalRows > options.ReviewThresholdRows || totalMb > options.ReviewThresholdMb)
            {
                logger.LogWarning(
                    "AuditLogs review threshold reached. Rows={Rows} SizeMb={SizeMb} ThresholdRows={ThresholdRows} ThresholdMb={ThresholdMb}",
                    totalRows,
                    Math.Round(totalMb, 2),
                    options.ReviewThresholdRows,
                    options.ReviewThresholdMb);
            }

            var outcome = $"Succeeded: purged={totalPurged}, rows={totalRows}, sizeMb={Math.Round(totalMb, 2)}";

            await auditLogService.WriteWithPolicyAsync(
                "system:worker",
                AuditLogCatalog.Action.SystemTaskSucceeded,
                AuditLogCatalog.Resource.SystemTask,
                AuditLogStatusEnum.Success,
                AuditMetadataFactory.CreateSystemTask(TaskName, executionId, outcome));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit retention cleanup worker failed. ExecutionId={ExecutionId}", executionId);

            await auditLogService.WriteWithPolicyAsync(
                "system:worker",
                AuditLogCatalog.Action.SystemTaskFailed,
                AuditLogCatalog.Resource.SystemTask,
                AuditLogStatusEnum.Failure,
                AuditMetadataFactory.CreateSystemTask(TaskName, executionId, "Failed", errorCode: ex.GetType().Name));
        }
    }
}
