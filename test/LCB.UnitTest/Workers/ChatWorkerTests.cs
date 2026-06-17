using System;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LCB.Application.Services;
using LCB.Application.Workers;
using LCB.Domain.Models;
using LCB.Domain.Models.Config;
using LCB.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LCB.UnitTest.Workers;

public class ChatWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_Returns_WhenTikTokUsernameIsMissing()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        channel.Writer.Complete();

        var worker = new ChatWorker(
            new TikTokChatProvider(channel.Writer, NullLogger<TikTokChatProvider>.Instance),
            new ChatProcessorService(channel.Reader, NullLogger<ChatProcessorService>.Instance),
            NullLogger<ChatWorker>.Instance,
            new TestOptionsMonitor(new LiveConfig { Tiktok = string.Empty }));

        var method = typeof(ChatWorker).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var task = (Task)method!.Invoke(worker, [CancellationToken.None])!;
        await task;
    }

    private sealed class TestOptionsMonitor(LiveConfig value) : IOptionsMonitor<LiveConfig>
    {
        public LiveConfig CurrentValue { get; } = value;

        public LiveConfig Get(string name)
            => CurrentValue;

        public IDisposable OnChange(Action<LiveConfig, string> listener)
            => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}