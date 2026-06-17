using System;
using System.Linq;
using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class ChatMessageRepositoryTests
{
    private static ChatMessageEntity NewMessage(ProviderTypeEnum provider, string author)
    {
        var message = new ChatMessageEntity
        {
            Provider = provider,
            Author = author,
            Text = "hello",
            Timestamp = DateTime.UtcNow
        };

        message.EnsureIdempotencyKey();
        return message;
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Flow_Works()
    {
        using var db = RepositoryTestDbFactory.CreateContext();
        var repo = new ChatMessageRepository(db.Context, new NullLogger<ChatMessageRepository>());

        var m1 = NewMessage(ProviderTypeEnum.TIKTOK, "alice");
        var m2 = NewMessage(ProviderTypeEnum.TWITCH, "bob");

        var created = await repo.CreateAsync([m1, m2]);
        var all = (await repo.GetAllAsync()).ToList();
        var byProvider = (await repo.GetByProviderAsync(ProviderTypeEnum.TIKTOK)).ToList();
        var found = await repo.GetByIdempotencyKeyAsync(m1.IdempotencyKey);

        m1.Text = "updated";
        var updated = await repo.UpdateAsync([m1]);
        var deleted = await repo.DeleteAsync(m1);
        var cleared = await repo.DeleteAllAsync();
        var after = (await repo.GetAllAsync()).ToList();

        Assert.True(created);
        Assert.Equal(2, all.Count);
        Assert.Single(byProvider);
        Assert.NotNull(found);
        Assert.True(updated);
        Assert.True(deleted);
        Assert.True(cleared);
        Assert.Empty(after);
    }
}