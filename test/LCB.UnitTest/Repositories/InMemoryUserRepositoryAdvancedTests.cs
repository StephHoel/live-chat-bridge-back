using System.Collections.Generic;
using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class InMemoryUserRepositoryAdvancedTests
{
    [Fact]
    public async Task GetByEmail_ReturnsCreatedUser_AndReturnsNullForMissingUser()
    {
        var repo = new InMemoryUserRepository(new NullLogger<InMemoryUserRepository>());

        var created = await repo.CreateAsync([UserEntity.Create("alice@example.com", "hash-a")]);
        var found = await repo.GetByEmailAsync("alice@example.com");
        var missing = await repo.GetByEmailAsync("missing@example.com");

        Assert.True(created);
        Assert.NotNull(found);
        Assert.Equal("alice@example.com", found!.Email);
        Assert.Null(missing);
    }

    [Fact]
    public async Task Create_AcceptsMultipleBatches()
    {
        var repo = new InMemoryUserRepository(new NullLogger<InMemoryUserRepository>());

        var first = await repo.CreateAsync([UserEntity.Create("u1@example.com", "h1")]);
        var second = await repo.CreateAsync([UserEntity.Create("u2@example.com", "h2"), UserEntity.Create("u3@example.com", "h3")]);

        Assert.True(first);
        Assert.True(second);
        Assert.NotNull(await repo.GetByEmailAsync("u2@example.com"));
        Assert.NotNull(await repo.GetByEmailAsync("u3@example.com"));
    }
}