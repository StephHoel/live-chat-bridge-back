using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LCB.Application.Workers;
using LCB.Domain.Constants;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Models.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LCB.UnitTest.Workers;

public class AuditRetentionCleanupWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsImmediately_WhenCancellationIsRequested()
    {
        var repository = new Mock<IAuditLogRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var worker = CreateWorker(repository.Object, auditLogService.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var method = typeof(AuditRetentionCleanupWorker)
            .GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task)method!.Invoke(worker, [cts.Token])!;
        await task;
    }

    [Fact]
    public async Task ExecuteCycleAsync_Skips_WhenLeaseIsNotAcquired()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository
            .Setup(x => x.TryAcquireMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var auditLogService = new Mock<IAuditLogService>();

        var worker = CreateWorker(repository.Object, auditLogService.Object);

        await InvokeExecuteCycleAsync(worker, CancellationToken.None);

        auditLogService.Verify(x => x.WriteWithPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AuditLogStatusEnum>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>()),
            Times.Never);

        repository.Verify(x => x.ReleaseMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteCycleAsync_WritesStartAndSuccess_AndReleasesLease_WhenCycleSucceeds()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository
            .Setup(x => x.TryAcquireMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repository
            .SetupSequence(x => x.PurgeExpiredAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .ReturnsAsync(2)
            .ReturnsAsync(0);

        repository
            .Setup(x => x.CountAsync())
            .ReturnsAsync(10);

        repository
            .Setup(x => x.GetDatabaseSizeMbAsync())
            .ReturnsAsync(1.5);

        repository
            .Setup(x => x.ReleaseMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auditLogService = new Mock<IAuditLogService>();
        auditLogService
            .Setup(x => x.WriteWithPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AuditLogStatusEnum>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(true);

        var worker = CreateWorker(repository.Object, auditLogService.Object);

        await InvokeExecuteCycleAsync(worker, CancellationToken.None);

        auditLogService.Verify(x => x.WriteWithPolicyAsync(
                "system:worker",
                AuditLogCatalog.Action.SystemTaskStarted,
                AuditLogCatalog.Resource.SystemTask,
                AuditLogStatusEnum.Info,
                It.IsAny<string>(),
                It.IsAny<DateTime?>()),
            Times.Once);

        auditLogService.Verify(x => x.WriteWithPolicyAsync(
                "system:worker",
                AuditLogCatalog.Action.SystemTaskSucceeded,
                AuditLogCatalog.Resource.SystemTask,
                AuditLogStatusEnum.Success,
                It.IsAny<string>(),
                It.IsAny<DateTime?>()),
            Times.Once);

        repository.Verify(x => x.ReleaseMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteCycleAsync_WritesFailed_AndReleasesLease_WhenCycleThrows()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository
            .Setup(x => x.TryAcquireMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        repository
            .Setup(x => x.PurgeExpiredAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        repository
            .Setup(x => x.ReleaseMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var auditLogService = new Mock<IAuditLogService>();
        auditLogService
            .Setup(x => x.WriteWithPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<AuditLogStatusEnum>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(true);

        var worker = CreateWorker(repository.Object, auditLogService.Object);

        await InvokeExecuteCycleAsync(worker, CancellationToken.None);

        auditLogService.Verify(x => x.WriteWithPolicyAsync(
                "system:worker",
                AuditLogCatalog.Action.SystemTaskStarted,
                AuditLogCatalog.Resource.SystemTask,
                AuditLogStatusEnum.Info,
                It.IsAny<string>(),
                It.IsAny<DateTime?>()),
            Times.Once);

        auditLogService.Verify(x => x.WriteWithPolicyAsync(
                "system:worker",
                AuditLogCatalog.Action.SystemTaskFailed,
                AuditLogCatalog.Resource.SystemTask,
                AuditLogStatusEnum.Failure,
                It.IsAny<string>(),
                It.IsAny<DateTime?>()),
            Times.Once);

        repository.Verify(x => x.ReleaseMaintenanceLeaseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AuditRetentionCleanupWorker CreateWorker(
        IAuditLogRepository repository,
        IAuditLogService auditLogService)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IAuditLogRepository)))
            .Returns(repository);
        serviceProvider
            .Setup(x => x.GetService(typeof(IAuditLogService)))
            .Returns(auditLogService);

        var scope = new Mock<IServiceScope>();
        scope
            .SetupGet(x => x.ServiceProvider)
            .Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory
            .Setup(x => x.CreateScope())
            .Returns(scope.Object);

        var options = Options.Create(new AuditRetentionPolicy
        {
            EndpointOperationalTtlDays = 30,
            WorkerFlowTtlDays = 15,
            SystemTaskTtlDays = 60,
            BatchSize = 1000,
            CleanupIntervalHours = 24,
            ReviewThresholdRows = 500000,
            ReviewThresholdMb = 256
        });

        return new AuditRetentionCleanupWorker(
            scopeFactory.Object,
            options,
            NullLogger<AuditRetentionCleanupWorker>.Instance);
    }

    private static async Task InvokeExecuteCycleAsync(AuditRetentionCleanupWorker worker, CancellationToken cancellationToken)
    {
        var method = typeof(AuditRetentionCleanupWorker)
            .GetMethod("ExecuteCycleAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task)method!.Invoke(worker, [cancellationToken])!;
        Assert.NotNull(task);

        await task!;
    }
}
