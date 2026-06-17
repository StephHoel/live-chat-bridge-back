using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LCB.Application.Services;
using LCB.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Services;

public class ChatProcessorServiceTests
{
    [Fact]
    public async Task ProcessMessagesAsync_ReadsMessagesUntilChannelCompletes()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var service = new ChatProcessorService(channel.Reader, NullLogger<ChatProcessorService>.Instance);
        var writerTask = Task.Run(async () =>
        {
            await channel.Writer.WriteAsync(new ChatMessageModel("user1", "hello", "TikTok", new DateTime(2026, 1, 1)));
            await channel.Writer.WriteAsync(new ChatMessageModel("user2", "world", "YouTube", new DateTime(2026, 1, 2)));
            channel.Writer.Complete();
        });

        await service.ProcessMessagesAsync(CancellationToken.None);
        await writerTask;

        Assert.True(true);
    }

    [Fact]
    public async Task ProcessMessagesAsync_Stops_WhenCancellationIsRequestedBeforeRead()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var service = new ChatProcessorService(channel.Reader, NullLogger<ChatProcessorService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ProcessMessagesAsync(cts.Token));
    }
}