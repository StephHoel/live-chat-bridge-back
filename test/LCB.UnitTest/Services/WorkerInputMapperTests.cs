using System;
using LCB.Application.Services;
using LCB.Domain.Enums;
using LCB.Domain.Models;
using Xunit;

namespace LCB.UnitTest.Services;

public class WorkerInputMapperTests
{
    [Fact]
    public void ToChatMessageEntity_MapsWorkerInputToEntity_AndNormalizesTimestampToUtcMinus3()
    {
        var workerInput = new ChatMessageModel(
            "alice",
            "hello",
            "TikTok",
            new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc),
            "worker-owner@example.com");

        var message = workerInput.ToChatMessageEntity();

        Assert.Equal(ProviderTypeEnum.TIKTOK, message.Provider);
        Assert.Equal("alice", message.Author);
        Assert.Equal("worker-owner@example.com", message.InsertedByUser);
        Assert.Equal("hello", message.Text);
        Assert.Equal(new DateTime(2026, 1, 1, 12, 0, 0), message.Timestamp);
        Assert.False(string.IsNullOrWhiteSpace(message.IdempotencyKey));
        Assert.Contains("-03:00", message.IdempotencyKey, StringComparison.Ordinal);
    }

    [Fact]
    public void ToChatMessageEntity_FallbacksProviderToTikTok_WhenPlatformIsUnknown()
    {
        var workerInput = new ChatMessageModel("alice", "hello", "Mixer", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc), "worker-owner@example.com");

        var message = workerInput.ToChatMessageEntity();

        Assert.Equal(ProviderTypeEnum.TIKTOK, message.Provider);
    }
}
