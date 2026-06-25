using System;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Enums;
using LCB.Domain.Extensions;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class MessageIngestMapperTests
{
    [Fact]
    public void ToChatMessage_MapsExplicitTimestampAndFields()
    {
        var timestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var request = new MessageIngestRequest(ProviderTypeEnum.YOUTUBE, "alice", "hello", timestamp);

        var message = request.ToChatMessage();

        Assert.Equal(ProviderTypeEnum.YOUTUBE, message.Provider);
        Assert.Equal("alice", message.Author);
        Assert.Equal("hello", message.Text);
        Assert.Equal(timestamp.NormalizeToUtcMinus3(), message.Timestamp);
    }

    [Fact]
    public void ToChatMessage_UsesUtcNow_WhenTimestampIsNull()
    {
        var before = DateTime.UtcNow.NormalizeToUtcMinus3();
        var request = new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "alice", "hello", null);

        var message = request.ToChatMessage();

        Assert.Equal(ProviderTypeEnum.TIKTOK, message.Provider);
        Assert.Equal("alice", message.Author);
        Assert.Equal("hello", message.Text);
        Assert.InRange(message.Timestamp, before.AddSeconds(-1), DateTime.UtcNow.NormalizeToUtcMinus3().AddSeconds(1));
    }
}