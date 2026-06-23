using System;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using Xunit;

namespace LCB.UnitTest.Entities;

public class ChatMessageEntityTests
{
    [Fact]
    public void EnsureIdempotencyKey_UsesProviderAuthorAndTimestamp()
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var first = new ChatMessageEntity
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Provider = ProviderTypeEnum.TIKTOK,
            Author = "alice",
            Timestamp = timestamp,
        };

        var second = new ChatMessageEntity
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Provider = ProviderTypeEnum.TIKTOK,
            Author = "alice",
            Timestamp = timestamp,
        };

        first.EnsureIdempotencyKey();
        second.EnsureIdempotencyKey();

        Assert.Equal(first.IdempotencyKey, second.IdempotencyKey);
        Assert.DoesNotContain(first.Id.ToString(), first.IdempotencyKey, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TIKTOK:alice:", first.IdempotencyKey, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureIdempotencyKey_DoesNotOverrideExistingKey()
    {
        var message = new ChatMessageEntity
        {
            IdempotencyKey = "custom-key",
            Provider = ProviderTypeEnum.YOUTUBE,
            Author = "alice",
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        message.EnsureIdempotencyKey();

        Assert.Equal("custom-key", message.IdempotencyKey);
    }
}
