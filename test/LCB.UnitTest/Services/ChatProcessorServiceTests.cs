using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LCB.Application.Commands.Message.Ingest;
using LCB.Application.Services;
using LCB.Domain.DTO;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LCB.UnitTest.Services;

public class ChatProcessorServiceTests
{
    [Fact]
    public async Task ProcessMessagesAsync_ProcessesValidMessage_UsingIngestUseCase()
    {
        var sut = CreateSut();
        List<ChatMessageEntity> createdMessages = [];

        sut.MessageRepository
            .Setup(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()))
            .Callback<IEnumerable<ChatMessageEntity>>(messages => createdMessages.AddRange(messages))
            .ReturnsAsync(true);

        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("user1", "hello", "TikTok", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        sut.Channel.Writer.Complete();

        await sut.Service.ProcessMessagesAsync(CancellationToken.None);

        sut.MessageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Once);
        var persisted = Assert.Single(createdMessages);
        Assert.Equal("user1", persisted.Author);
        Assert.Equal("hello", persisted.Text);
        Assert.NotEqual(default, persisted.Timestamp);
        Assert.NotEmpty(persisted.IdempotencyKey);
    }

    [Fact]
    public async Task ProcessMessagesAsync_DiscardsInvalidMessage_WithoutInvokingIngestUseCase()
    {
        var sut = CreateSut();

        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("   ", "hello", "TikTok", DateTime.UtcNow));
        sut.Channel.Writer.Complete();

        await sut.Service.ProcessMessagesAsync(CancellationToken.None);

        sut.MessageRepository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Never);
        sut.MessageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Never);
        sut.AdapterService.Verify(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessagesAsync_RetriesTransientFailure_AndContinuesWithNextMessage()
    {
        var sut = CreateSut();

        sut.AdapterService
            .Setup(a => a.ParseAndDispatch(It.Is<ChatMessageEntity>(m => m.Text == "boom")))
            .ThrowsAsync(new InvalidOperationException("temporary"));

        sut.AdapterService
            .Setup(a => a.ParseAndDispatch(It.Is<ChatMessageEntity>(m => m.Text == "ok")))
            .ReturnsAsync(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-ok"));

        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("user1", "boom", "TikTok", DateTime.UtcNow));
        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("user2", "ok", "TikTok", DateTime.UtcNow));
        sut.Channel.Writer.Complete();

        await sut.Service.ProcessMessagesAsync(CancellationToken.None);

        sut.AdapterService.Verify(a => a.ParseAndDispatch(It.Is<ChatMessageEntity>(m => m.Text == "boom")), Times.Exactly(3));
        sut.AdapterService.Verify(a => a.ParseAndDispatch(It.Is<ChatMessageEntity>(m => m.Text == "ok")), Times.Once);
        sut.MessageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessagesAsync_RetriesWhenPersistenceFails_AndSucceedsOnThirdAttempt()
    {
        var sut = CreateSut();

        sut.MessageRepository
            .SetupSequence(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()))
            .ReturnsAsync(false)
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("user-persist", "hello", "TikTok", DateTime.UtcNow));
        sut.Channel.Writer.Complete();

        await sut.Service.ProcessMessagesAsync(CancellationToken.None);

        sut.MessageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Exactly(3));
        sut.AdapterService.Verify(a => a.ParseAndDispatch(It.Is<ChatMessageEntity>(m => m.Author == "user-persist")), Times.Exactly(3));
    }

    [Fact]
    public async Task ProcessMessagesAsync_StopsOnDuplicate_WithoutRetryingOrDispatchingCommand()
    {
        var sut = CreateSut();

        var duplicate = new ChatMessageEntity
        {
            Provider = ProviderTypeEnum.TIKTOK,
            Author = "alice",
            Text = "hello",
            Timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Processed = true,
        };
        duplicate.EnsureIdempotencyKey();

        sut.MessageRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync(duplicate);

        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("alice", "hello", "TikTok", duplicate.Timestamp));
        sut.Channel.Writer.Complete();

        await sut.Service.ProcessMessagesAsync(CancellationToken.None);

        sut.MessageRepository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()), Times.Once);
        sut.AdapterService.Verify(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()), Times.Never);
        sut.MessageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Never);
        sut.MessageRepository.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessagesAsync_UpdatesQueue_WhenMessageMatchesQueuePolicy()
    {
        var sut = CreateSut();

        await sut.Channel.Writer.WriteAsync(new ChatMessageModel("queue-user", "!fila", "TikTok", DateTime.UtcNow));
        sut.Channel.Writer.Complete();

        await sut.Service.ProcessMessagesAsync(CancellationToken.None);

        sut.QueueRepository.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<QueueEntity>>()), Times.Once);
        sut.MessageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessagesAsync_Stops_WhenCancellationIsRequestedBeforeRead()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var provider = new ServiceCollection().BuildServiceProvider();
        var service = new ChatProcessorService(channel.Reader,
                                               provider.GetRequiredService<IServiceScopeFactory>(),
                                               NullLogger<ChatProcessorService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ProcessMessagesAsync(cts.Token));
    }

    private static ChatProcessorServiceSut CreateSut()
    {
        var channel = Channel.CreateUnbounded<ChatMessageModel>();
        var messageRepository = new Mock<IMessageRepository>();
        var queueRepository = new Mock<IQueueRepository>();
        var adapterService = new Mock<IAdapterService>();

        messageRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((ChatMessageEntity)null);
        messageRepository
            .Setup(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()))
            .ReturnsAsync(true);
        messageRepository
            .Setup(r => r.UpdateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()))
            .ReturnsAsync(true);

        queueRepository
            .Setup(r => r.GetByUserAsync(It.IsAny<string>()))
            .ReturnsAsync((QueueEntity)null);
        queueRepository
            .Setup(r => r.UpdateAsync(It.IsAny<IEnumerable<QueueEntity>>()))
            .ReturnsAsync(true);

        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ReturnsAsync(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-default"));

        var provider = new ServiceCollection()
            .AddLogging()
            .AddScoped(_ => messageRepository.Object)
            .AddScoped(_ => queueRepository.Object)
            .AddScoped(_ => adapterService.Object)
            .AddScoped<MessageIngestHandler>()
            .BuildServiceProvider();

        var processor = new ChatProcessorService(channel.Reader,
                                                 provider.GetRequiredService<IServiceScopeFactory>(),
                                                 NullLogger<ChatProcessorService>.Instance);

        return new ChatProcessorServiceSut(processor, channel, messageRepository, queueRepository, adapterService);
    }

    private sealed record ChatProcessorServiceSut(
        ChatProcessorService Service,
        Channel<ChatMessageModel> Channel,
        Mock<IMessageRepository> MessageRepository,
        Mock<IQueueRepository> QueueRepository,
        Mock<IAdapterService> AdapterService);
}