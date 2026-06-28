using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Infrastructure.Repositories;
using LCB.UnitTest.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class LiveSettingsRepositoryTests
{
    [Fact]
    public async Task Upsert_Creates_And_Updates_Settings_ByUser()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var userRepo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var repo = new LiveSettingsRepository(db.Context, new NullLogger<LiveSettingsRepository>());

        var user = UserEntity.Create("alice@example.com", "hash");
        await userRepo.CreateAsync([user]);

        var createdSettings = LiveSettingsEntity.Create(user.Id, user.Email, "alice-live", null, null, 5);
        var created = await repo.UpsertAsync(createdSettings);
        var persisted = await repo.GetByUserIdAsync(user.Id);

        Assert.True(created);
        Assert.NotNull(persisted);
        Assert.Equal("alice-live", persisted!.TikTokUsername);
        Assert.Equal(5, persisted.ReloadTimeInSec);
        Assert.Equal("alice@example.com", persisted.UpdatedByUser);

        persisted.SetTikTokUsername("updated-live");
        persisted.SetReloadTimeInSec(10);
        persisted.TouchUpdatedBy("alice@example.com");

        var updated = await repo.UpsertAsync(persisted);
        var afterUpdate = await repo.GetByUserIdAsync(user.Id);

        Assert.True(updated);
        Assert.NotNull(afterUpdate);
        Assert.Equal("updated-live", afterUpdate!.TikTokUsername);
        Assert.Equal(10, afterUpdate.ReloadTimeInSec);
    }
}
