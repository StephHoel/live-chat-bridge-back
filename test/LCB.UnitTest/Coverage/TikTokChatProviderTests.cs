#nullable enable
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LCB.Domain.Models;
using LCB.Infrastructure.Providers;
using LCB.UnitTest.Factories;
using LCB.UnitTest.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using TikTokLiveSharp.Events;
using TikTokLiveSharp.Events.Objects;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class TikTokChatProviderTests
{
    [Fact]
    public void PrivateHandlers_AreExercised_UsingMockClient()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var provider = new TikTokChatProvider(channel.Writer, new NullLogger<TikTokChatProvider>());
        var mockClient = TikTokClientMockFactory.CreateMockClient();

        var chat = ReflectionTestHelper.CreateInstance<Chat>();
        ReflectionTestHelper.SetMemberValue(chat, "Message", "hello from chat");
        ReflectionTestHelper.SetMemberValue(chat, "TimeStamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var chatSender = ReflectionTestHelper.CreateNestedMemberInstance(chat, "Sender");
        ReflectionTestHelper.SetMemberValue(chatSender, "UniqueId", "tester123");
        ReflectionTestHelper.SetMemberValue(chat, "Sender", chatSender);

        ReflectionTestHelper.InvokePrivate(provider, "OnChatMessage", chat, "worker-owner@example.com");
        Assert.True(channel.Reader.TryRead(out var written));
        Assert.Equal("tester123", written!.User);
        Assert.Equal("hello from chat", written.Text);
        Assert.Equal("TikTok", written.Platform);
        Assert.Equal("worker-owner@example.com", written.InsertedByUser);

        var gift = ReflectionTestHelper.CreateInstance<TikTokGift>();
        var giftObj = ReflectionTestHelper.CreateNestedMemberInstance(gift, "Gift");
        ReflectionTestHelper.SetMemberValue(giftObj, "Name", "Rose");
        ReflectionTestHelper.SetMemberValue(gift, "Gift", giftObj);
        ReflectionTestHelper.SetMemberValue(gift, "Amount", 2);
        var giftSender = ReflectionTestHelper.CreateNestedMemberInstance(gift, "Sender");
        ReflectionTestHelper.SetMemberValue(giftSender, "NickName", "nick");
        ReflectionTestHelper.SetMemberValue(gift, "Sender", giftSender);

        ReflectionTestHelper.InvokePrivate(provider, "OnGift", mockClient, gift);

        var giftWithNulls = ReflectionTestHelper.CreateInstance<TikTokGift>();
        ReflectionTestHelper.SetMemberValue(giftWithNulls, "Gift", null);
        ReflectionTestHelper.SetMemberValue(giftWithNulls, "Sender", null);
        ReflectionTestHelper.InvokePrivate(provider, "OnGift", mockClient, giftWithNulls);

        var like = ReflectionTestHelper.CreateInstance<Like>();
        ReflectionTestHelper.SetMemberValue(like, "Count", 3);
        ReflectionTestHelper.SetMemberValue(like, "Total", 10);
        var likeSender = ReflectionTestHelper.CreateNestedMemberInstance(like, "Sender");
        ReflectionTestHelper.SetMemberValue(likeSender, "UniqueId", "user-like");
        ReflectionTestHelper.SetMemberValue(like, "Sender", likeSender);

        ReflectionTestHelper.InvokePrivate(provider, "OnLike", mockClient, like);

        var likeWithNullSender = ReflectionTestHelper.CreateInstance<Like>();
        ReflectionTestHelper.SetMemberValue(likeWithNullSender, "Sender", null);
        ReflectionTestHelper.InvokePrivate(provider, "OnLike", mockClient, likeWithNullSender);

        ReflectionTestHelper.InvokePrivate(provider, "OnException", null, new Exception("x"));
    }

    [Fact]
    public async Task Connect_DoesNotThrow_WhenAlreadyCancelled()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var provider = new TikTokChatProvider(channel.Writer, new NullLogger<TikTokChatProvider>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        provider.Connect("unit-test-user", "worker-owner@example.com", cts.Token);

        using var delayedCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var connectTask = Task.Run(() => provider.Connect("unit-test-user", "worker-owner@example.com", delayedCts.Token));

        var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(8)));
        Assert.Same(connectTask, completed);
    }

    [Fact]
    public async Task Connect_CanBeStopped_IfClientStarts()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var provider = new TikTokChatProvider(channel.Writer, new NullLogger<TikTokChatProvider>());

        using var cts = new CancellationTokenSource();
        var connectTask = Task.Run(() => provider.Connect("unit-test-user", "worker-owner@example.com", cts.Token));

        // Allow Connect to start and then request stop through the cancellation token.
        await Task.Delay(100);
        cts.Cancel();

        var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(connectTask, completed);
    }
}
