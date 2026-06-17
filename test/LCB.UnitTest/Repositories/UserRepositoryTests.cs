using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task Create_And_GetByEmail_Works()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new UserRepository(db.Context, new NullLogger<UserRepository>());

        var users = new[]
        {
            UserEntity.Create("alice@example.com", "hash-a"),
            UserEntity.Create("bob@example.com", "hash-b")
        };

        var created = await repo.CreateAsync(users);
        var found = await repo.GetByEmailAsync("alice@example.com");

        Assert.True(created);
        Assert.NotNull(found);
        Assert.Equal("alice@example.com", found!.Email);
    }

    [Fact]
    public async Task GetByEmail_ReturnsNull_WhenUserDoesNotExist()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new UserRepository(db.Context, new NullLogger<UserRepository>());

        var found = await repo.GetByEmailAsync("missing@example.com");

        Assert.Null(found);
    }
}