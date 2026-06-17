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

public class QueueRepositoryTests
{
    private static QueueEntity NewQueue(ProviderTypeEnum provider, string user)
        => new(null, provider, user, false, DateTime.UtcNow);

    [Fact]
    public async Task Update_Get_Delete_Flow_Works()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new QueueRepository(db.Context, new NullLogger<QueueRepository>());

        var q1 = NewQueue(ProviderTypeEnum.TIKTOK, "alice");
        var q2 = NewQueue(ProviderTypeEnum.TWITCH, "bob");

        var updated = await repo.UpdateAsync([q1, q2]);
        var all = (await repo.GetAllAsync()).ToList();
        var bob = await repo.GetByUserAsync("bob");
        var exists = await repo.UserExistsAsync("alice");
        var deleted = await repo.DeleteAsync(q1);
        var cleared = await repo.DeleteAllAsync();
        var after = (await repo.GetAllAsync()).ToList();

        Assert.True(updated);
        Assert.Equal(2, all.Count);
        Assert.NotNull(bob);
        Assert.True(exists);
        Assert.True(deleted);
        Assert.True(cleared);
        Assert.Empty(after);
    }
}