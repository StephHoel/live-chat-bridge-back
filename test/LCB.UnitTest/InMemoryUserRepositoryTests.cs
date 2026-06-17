using System.Collections.Generic;
using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.Infrastructure.Tests;

public class InMemoryUserRepositoryTests
{
    private static InMemoryUserRepository CreateRepo()
        => new(new NullLogger<InMemoryUserRepository>());

    private static UserEntity NewUser(string email, string passwordHash = "h")
        => UserEntity.Create(email, passwordHash);

    [Fact]
    public async Task Create_And_GetByEmail_Works()
    {
        var repo = CreateRepo();
        var u1 = NewUser("alice@example.com", "pwd1");
        var u2 = NewUser("bob@example.com", "pwd2");

        var created = await repo.CreateAsync([u1, u2]);
        Assert.True(created);

        var found = await repo.GetByEmailAsync("alice@example.com");
        Assert.NotNull(found);
        Assert.Equal("alice@example.com", found!.Email);
    }

    [Fact]
    public async Task Concurrent_Create_AddsAll()
    {
        var repo = CreateRepo();
        var tasks = new List<Task<bool>>();
        const int parallel = 30;

        for (int i = 0; i < parallel; i++)
        {
            var email = $"u{i}@test.local";
            tasks.Add(repo.CreateAsync([NewUser(email)]));
        }

        var results = await Task.WhenAll(tasks);
        Assert.Equal(parallel, results.Length);
        Assert.All(results, Assert.True);
    }
}