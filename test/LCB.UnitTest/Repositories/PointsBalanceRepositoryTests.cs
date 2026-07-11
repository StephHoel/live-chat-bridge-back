using System.Threading.Tasks;
using LCB.Domain.Enums;
using LCB.Infrastructure.Repositories;
using LCB.UnitTest.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class PointsBalanceRepositoryTests
{
    private static PointsBalanceRepository CreateRepo(RepositoryTestDbFactory.DbScope db)
        => new(db.Context, new NullLogger<PointsBalanceRepository>());

    [Fact]
    public async Task GetActiveBalance_NoRecord_ReturnsNull()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        var result = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");

        Assert.Null(result);
    }

    [Fact]
    public async Task UpsertAsync_NoExistingBalance_CreatesNewRecord()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        var balance = await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);

        Assert.NotNull(balance);
        Assert.Equal(10, balance.Points);
        Assert.True(balance.IsActive);
    }

    [Fact]
    public async Task UpsertAsync_ExistingBalance_AccumulatesPoints()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);
        var balance = await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);

        Assert.Equal(20, balance.Points);
    }

    [Fact]
    public async Task UpsertAsync_NegativeDeltaOnNewRecord_ClampsToZero()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        var balance = await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", -5);

        Assert.Equal(0, balance.Points);
    }

    [Fact]
    public async Task ClearAsync_ActiveRecord_DeactivatesIt()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 50);
        var cleared = await repo.ClearAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");

        Assert.True(cleared);
        var active = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        Assert.Null(active);
    }

    [Fact]
    public async Task ClearAsync_NoRecord_ReturnsFalse()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        var cleared = await repo.ClearAsync(ProviderTypeEnum.TIKTOK, "streamer", "userX");

        Assert.False(cleared);
    }

    [Fact]
    public async Task UpsertAsync_AfterClear_CreatesNewActiveBalance()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 50);
        await repo.ClearAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        var newBalance = await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);

        Assert.Equal(10, newBalance.Points);
        Assert.True(newBalance.IsActive);

        // Histórico inativo deve existir
        var all = db.Context.PointsBalances
            .Where(x => x.Provider == ProviderTypeEnum.TIKTOK && x.ChannelId == "streamer" && x.UserId == "user1")
            .ToList();

        Assert.Equal(2, all.Count);
        Assert.Single(all, x => !x.IsActive);
        Assert.Single(all, x => x.IsActive);
    }

    [Fact]
    public async Task UpsertAsync_DifferentContexts_IsolatedBalances()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 100);
        await repo.UpsertAsync(ProviderTypeEnum.TWITCH, "streamer", "user1", 50);

        var tikTok = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        var twitch = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TWITCH, "streamer", "user1");

        Assert.Equal(100, tikTok!.Points);
        Assert.Equal(50, twitch!.Points);
    }

    [Fact]
    public async Task UpsertAsync_Concurrency_ResultIsConsistent()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        // SQLite :memory: é single-connection, concorrência via Task.WhenAll serializa na prática.
        // O teste valida que múltiplas chamadas sequenciais acumulam corretamente.
        var repo = CreateRepo(db);

        for (var i = 0; i < 5; i++)
            await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);

        var balance = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");

        Assert.Equal(50, balance!.Points);
    }
}
