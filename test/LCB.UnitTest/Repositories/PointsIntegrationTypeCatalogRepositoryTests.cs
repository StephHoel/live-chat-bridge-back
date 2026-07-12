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

public class PointsIntegrationTypeCatalogRepositoryTests
{
    private static PointsIntegrationTypeCatalogRepository CreateRepo(RepositoryTestDbFactory.DbScope db)
        => new(db.Context, new NullLogger<PointsIntegrationTypeCatalogRepository>());

    [Fact]
    public async Task GetDeltaAsync_NoRule_ReturnsZeroFallback()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        var delta = await repo.GetDeltaAsync(Guid.NewGuid(), ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Message);

        Assert.Equal(0, delta);
    }

    [Fact]
    public async Task UpsertAsync_NewRule_PersistsAndCanBeRead()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);
        var streamerUserId = Guid.NewGuid();

        var rule = PointsIntegrationTypeCatalogEntity.Create(
            streamerUserId,
            ProviderTypeEnum.TIKTOK,
            IntegrationTypeEnum.Message,
            25);

        var saved = await repo.UpsertAsync(rule);
        var delta = await repo.GetDeltaAsync(streamerUserId, ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Message);

        Assert.True(saved);
        Assert.Equal(25, delta);
    }

    [Fact]
    public async Task UpsertAsync_ExistingRule_UpdatesDeltaWithoutCreatingDuplicate()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);
        var streamerUserId = Guid.NewGuid();

        await repo.UpsertAsync(PointsIntegrationTypeCatalogEntity.Create(
            streamerUserId,
            ProviderTypeEnum.TIKTOK,
            IntegrationTypeEnum.Like,
            2));

        var updated = await repo.UpsertAsync(PointsIntegrationTypeCatalogEntity.Create(
            streamerUserId,
            ProviderTypeEnum.TIKTOK,
            IntegrationTypeEnum.Like,
            7));

        var delta = await repo.GetDeltaAsync(streamerUserId, ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Like);
        var count = db.Context.PointsIntegrationTypeCatalog.Count();

        Assert.True(updated);
        Assert.Equal(7, delta);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpsertAsync_OneContextDoesNotAffectAnother()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);
        var streamerA = Guid.NewGuid();
        var streamerB = Guid.NewGuid();

        await repo.UpsertAsync(PointsIntegrationTypeCatalogEntity.Create(
            streamerA,
            ProviderTypeEnum.TIKTOK,
            IntegrationTypeEnum.Message,
            11));

        await repo.UpsertAsync(PointsIntegrationTypeCatalogEntity.Create(
            streamerB,
            ProviderTypeEnum.TIKTOK,
            IntegrationTypeEnum.Message,
            3));

        var deltaA = await repo.GetDeltaAsync(streamerA, ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Message);
        var deltaB = await repo.GetDeltaAsync(streamerB, ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Message);

        Assert.Equal(11, deltaA);
        Assert.Equal(3, deltaB);
    }
}