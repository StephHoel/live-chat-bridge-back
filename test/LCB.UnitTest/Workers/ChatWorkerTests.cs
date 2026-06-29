using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LCB.Application.Services;
using LCB.Application.Workers;
using LCB.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Workers;

public class ChatWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_Completes_WhenChannelIsCompleted()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        channel.Writer.Complete();
        var provider = new ServiceCollection().BuildServiceProvider();

        var worker = new ChatWorker(
            new ChatProcessorService(channel.Reader,
                                     provider.GetRequiredService<IServiceScopeFactory>(),
                                     NullLogger<ChatProcessorService>.Instance),
            NullLogger<ChatWorker>.Instance);

        var method = typeof(ChatWorker).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = (Task)method!.Invoke(worker, [CancellationToken.None])!;
        await task;
    }
}