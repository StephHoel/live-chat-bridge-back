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

public class InMemoryMessageRepositoryTests
{
    private static InMemoryMessageRepository CreateRepo()
        => new(new NullLogger<InMemoryMessageRepository>());

    private static ChatMessage NewMessage(ProviderTypeEnum provider, string author = "a")
    {
        return new ChatMessage
        {
            Provider = provider,
            Author = author,
            Text = "hello",
            Timestamp = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Work()
    {
        var repo = CreateRepo();
        var m1 = NewMessage(ProviderTypeEnum.TIKTOK, "alice");
        var m2 = NewMessage(ProviderTypeEnum.TWITCH, "bob");

        var c = await repo.CreateAsync([m1, m2]);
        Assert.True(c);

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);

        var byProvider = (await repo.GetByProviderAsync(ProviderTypeEnum.TIKTOK)).ToList();
        Assert.Single(byProvider);

        var found = await repo.GetByIdempotencyKeyAsync(m1.IdempotencyKey);
        Assert.NotNull(found);

        m1.Text = "updated";
        var u = await repo.UpdateAsync([m1]);
        Assert.True(u);

        var del = await repo.DeleteAsync(m1);
        Assert.True(del);

        var cleared = await repo.DeleteAllAsync();
        Assert.True(cleared);
        var after = (await repo.GetAllAsync()).ToList();
        Assert.Empty(after);
    }

    [Fact]
    public async Task Concurrent_Create_Works()
    {
        var repo = CreateRepo();
        var tasks = new List<Task<bool>>();
        const int parallel = 50;

        for (int i = 0; i < parallel; i++)
        {
            var author = $"u{i}";
            tasks.Add(repo.CreateAsync([NewMessage(ProviderTypeEnum.YOUTUBE, author)]));
        }

        var results = await Task.WhenAll(tasks);
        Assert.Equal(parallel, results.Length);
        Assert.All(results, Assert.True);

        var all = (await repo.GetAllAsync()).ToList();
        Assert.Equal(parallel, all.Count);
    }
}