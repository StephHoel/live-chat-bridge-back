using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.Infrastructure.Tests.Repositories;

public class InMemoryQueueRepositoryTests
{
    private static InMemoryQueueRepository CreateRepo()
        => new(new NullLogger<InMemoryQueueRepository>());

    private static Queue NewQueue(ProviderTypeEnum provider, string user = "u")
        => new(null, provider, user, false, DateTime.UtcNow);

    [Fact]
    public async Task Create_Update_Delete_Work()
    {
        var repo = CreateRepo();
        var q1 = NewQueue(ProviderTypeEnum.TIKTOK, "alice");
        var q2 = NewQueue(ProviderTypeEnum.TWITCH, "bob");

        var ok = await repo.UpdateAsync([q1, q2]);
        Assert.True(ok);

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Contains(all, x => x.User == "alice");

        var found = await repo.GetByUserAsync("bob");
        Assert.NotNull(found);

        var exists = await repo.UserExistsAsync("alice");
        Assert.True(exists);

        var del = await repo.DeleteAsync(q1);
        Assert.True(del);

        var cleared = await repo.DeleteAllAsync();
        Assert.True(cleared);
        var after = (await repo.GetAllAsync()).ToList();
        Assert.Empty(after);
    }

    [Fact]
    public async Task Concurrent_Updates_AddAll()
    {
        var repo = CreateRepo();
        var tasks = new List<Task<bool>>();
        const int parallel = 50;

        for (int i = 0; i < parallel; i++)
        {
            var user = $"u{i}";
            tasks.Add(repo.UpdateAsync([NewQueue(ProviderTypeEnum.YOUTUBE, user)]));
        }

        var results = await Task.WhenAll(tasks);
        Assert.Equal(parallel, results.Length);
        Assert.All(results, r => Assert.True(r));

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Equal(parallel, all.Count);
    }
}
