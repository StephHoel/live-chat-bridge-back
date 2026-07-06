using System;
using System.Linq;
using System.Threading.Tasks;
using LCB.Application.Commands.Queue.Get;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Infrastructure.Repositories;
using LCB.UnitTest.Factories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class GetQueueHandlerTests
{
    [Fact]
    public async Task Handle_Returns_Items_Ordered_By_CreatedAt()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repository = new QueueRepository(db.Context, new NullLogger<QueueRepository>());

        var newest = new QueueEntity(Guid.NewGuid(), ProviderTypeEnum.TWITCH, "charlie", false, new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc))
        {
            CreatedAt = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc)
        };

        var oldest = new QueueEntity(Guid.NewGuid(), ProviderTypeEnum.TIKTOK, "alice", false, new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc))
        {
            CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        };

        var middle = new QueueEntity(Guid.NewGuid(), ProviderTypeEnum.YOUTUBE, "bob", true, new DateTime(2026, 1, 1, 10, 2, 0, DateTimeKind.Utc))
        {
            CreatedAt = new DateTime(2026, 1, 1, 10, 2, 0, DateTimeKind.Utc)
        };

        await repository.UpdateAsync([newest, oldest, middle]);

        var handler = new GetQueueHandler(repository, new NullLogger<GetQueueHandler>());

        var result = await handler.Handle();

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(["alice", "bob", "charlie"], [.. result.Data!.Items.Select(x => x.User)]);
    }
}