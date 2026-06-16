using System;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class DomainEntitiesTests
{
    [Fact]
    public void ChatMessage_ExposesDefaults_AndIdempotencyKey()
    {
        var timestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var message = new ChatMessage
        {
            Provider = ProviderTypeEnum.TIKTOK,
            Author = "alice",
            Text = "hello",
            Timestamp = timestamp
        };

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal("TIKTOK:20260102030405:" + message.Id, message.IdempotencyKey);
        Assert.False(message.Processed);
    }

    [Fact]
    public void Queue_UsesConstructorValuesAndDefaults()
    {
        var before = DateTime.UtcNow;
        var id = Guid.NewGuid();
        var queue = new Queue(id, ProviderTypeEnum.YOUTUBE, "bob", null, null);

        Assert.Equal(id, queue.Id);
        Assert.Equal(ProviderTypeEnum.YOUTUBE, queue.Provider);
        Assert.Equal("bob", queue.User);
        Assert.False(queue.Selected);
        Assert.InRange(queue.JoinedAt, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void User_Create_PreservesValues()
    {
        var user = User.Create("alice@example.com", "hash");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("hash", user.PasswordHash);
    }
}