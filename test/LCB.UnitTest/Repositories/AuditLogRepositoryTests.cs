using System;
using System.Linq;
using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Infrastructure.Repositories;
using LCB.UnitTest.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class AuditLogRepositoryTests
{
    [Fact]
    public async Task Create_And_GetByPeriod_FilteredByActor_Works()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new AuditLogRepository(db.Context, new NullLogger<AuditLogRepository>());

        var now = DateTime.UtcNow;
        var first = AuditLogEntity.Create("alice@example.com", "worker.start", "worker", AuditLogStatusEnum.Success, "{\"attempt\":1}", now.AddMinutes(-3));
        var second = AuditLogEntity.Create("bob@example.com", "worker.stop", "worker", AuditLogStatusEnum.Warning, "{\"attempt\":2}", now.AddMinutes(-2));

        var createdFirst = await repo.CreateAsync(first);
        var createdSecond = await repo.CreateAsync(second);

        var periodStart = now.AddMinutes(-5);
        var periodEnd = now;

        var allInPeriod = (await repo.GetByPeriodAsync(periodStart, periodEnd)).ToList();
        var onlyAlice = (await repo.GetByPeriodAsync(periodStart, periodEnd, "alice@example.com")).ToList();

        Assert.True(createdFirst);
        Assert.True(createdSecond);
        Assert.Equal(2, allInPeriod.Count);
        Assert.Single(onlyAlice);
        Assert.Equal("alice@example.com", onlyAlice[0].ActorUser);
    }

    [Fact]
    public async Task Create_Persists_Status_AsString()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new AuditLogRepository(db.Context, new NullLogger<AuditLogRepository>());

        var auditLog = AuditLogEntity.Create(
            "alice@example.com",
            "worker.start",
            "worker",
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1}");

        var created = await repo.CreateAsync(auditLog);

        Assert.True(created);

        using var command = db.Connection.CreateCommand();
        command.CommandText = "SELECT Status FROM AuditLogs LIMIT 1";

        var status = (await command.ExecuteScalarAsync())?.ToString();

        Assert.Equal(nameof(AuditLogStatusEnum.Success), status);
    }

    [Fact]
    public async Task TryAcquireMaintenanceLease_PreventsConcurrentOwnership_AndAllowsAcquireAfterRelease()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new AuditLogRepository(db.Context, new NullLogger<AuditLogRepository>());

        var leaseName = "audit-retention-cleanup";
        var ownerA = "instance-a";
        var ownerB = "instance-b";

        var acquiredByA = await repo.TryAcquireMaintenanceLeaseAsync(leaseName, ownerA, TimeSpan.FromMinutes(2));
        var acquiredByBWhileHeld = await repo.TryAcquireMaintenanceLeaseAsync(leaseName, ownerB, TimeSpan.FromMinutes(2));
        var releasedByA = await repo.ReleaseMaintenanceLeaseAsync(leaseName, ownerA);
        var acquiredByBAfterRelease = await repo.TryAcquireMaintenanceLeaseAsync(leaseName, ownerB, TimeSpan.FromMinutes(2));

        Assert.True(acquiredByA);
        Assert.False(acquiredByBWhileHeld);
        Assert.True(releasedByA);
        Assert.True(acquiredByBAfterRelease);
    }

    [Fact]
    public async Task TryAcquireMaintenanceLease_AllowsTakeover_WhenLeaseExpires()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new AuditLogRepository(db.Context, new NullLogger<AuditLogRepository>());

        var leaseName = "audit-retention-cleanup";
        var ownerA = "instance-a";
        var ownerB = "instance-b";

        var acquiredByA = await repo.TryAcquireMaintenanceLeaseAsync(leaseName, ownerA, TimeSpan.FromMilliseconds(20));
        await Task.Delay(60);
        var acquiredByB = await repo.TryAcquireMaintenanceLeaseAsync(leaseName, ownerB, TimeSpan.FromMinutes(1));

        Assert.True(acquiredByA);
        Assert.True(acquiredByB);
    }
}
