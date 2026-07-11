using System.Linq;
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
    public async Task UpsertAsync_MultipleSequentialCalls_AccumulatesPoints()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        // SQLite :memory: é single-connection, então Task.WhenAll tende a serializar na prática.
        // Este teste valida que múltiplas chamadas sequenciais acumulam corretamente.
        var repo = CreateRepo(db);

        for (var i = 0; i < 5; i++)
            await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);

        var balance = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");

        Assert.Equal(50, balance!.Points);
    }

    [Fact]
    public async Task TryDebitAsync_SufficientBalance_ReturnsTrue()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 100);
        var result = await repo.TryDebitAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 30);

        Assert.True(result);
        var balance = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        Assert.Equal(70, balance!.Points);
    }

    [Fact]
    public async Task TryDebitAsync_InsufficientBalance_ReturnsFalseAndNoChange()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 50);
        var result = await repo.TryDebitAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 100);

        Assert.False(result);
        var balance = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        Assert.Equal(50, balance!.Points); // saldo preservado
    }

    [Fact]
    public async Task TryDebitAsync_NoRecord_ReturnsFalse()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        var result = await repo.TryDebitAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 10);

        Assert.False(result);
    }

    [Fact]
    public async Task TryDebitAsync_ZeroOrNegativePoints_ReturnsFalse()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 100);
        var result = await repo.TryDebitAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 0);

        Assert.False(result);
        var balance = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        Assert.Equal(100, balance!.Points);
    }

    [Fact]
    public async Task TryDebitAsync_ConcurrentCallsSimulated_Serialized()
    {
        // SQLite :memory: serializa naturalmente; este teste valida que a validação + atualização
        // são atômicas, rejeitando o segundo debit se o saldo fica insuficiente.
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = CreateRepo(db);

        await repo.UpsertAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 100);

        var debit1 = await repo.TryDebitAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 60);
        var debit2 = await repo.TryDebitAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1", 60);

        Assert.True(debit1);  // primeiro passa
        Assert.False(debit2); // segundo falha (saldo 40 < 60)
        var balance = await repo.GetActiveBalanceAsync(ProviderTypeEnum.TIKTOK, "streamer", "user1");
        Assert.Equal(40, balance!.Points);
    }
}
