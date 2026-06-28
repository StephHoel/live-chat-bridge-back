using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Config.Live.Get;
using LCB.Domain.Entities;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static LCB.UnitTest.Factories.RepositoryTestDbFactory;

namespace LCB.UnitTest.Handlers;

public class GetLiveConfigHandlerTests
{
    [Fact]
    public async Task Handle_Creates_Default_Record_WhenMissing()
    {
        using var db = CreateContext();
        var userRepo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var repo = new LiveSettingsRepository(db.Context, new NullLogger<LiveSettingsRepository>());
        var user = UserEntity.Create("alice@example.com", "hash");
        await userRepo.CreateAsync([user]);

        var handler = new GetLiveConfigHandler(repo, new NullLogger<GetLiveConfigHandler>());

        var result = await handler.Handle(user.Id, user.Email);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data!.ReloadTimeInSec);

        var persisted = await repo.GetByUserIdAsync(user.Id);
        Assert.NotNull(persisted);
        Assert.Equal(user.Email, persisted!.UpdatedByUser);
    }
}
