using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.DTO;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class MessageIngestHandlerTests
{
    private readonly MessageIngestHandler handler;
    private readonly Mock<IMessageRepository> messageRepository;
    private readonly Mock<IQueueRepository> queueRepository;
    private readonly Mock<IAdapterService> adapterService;
    private readonly Mock<ILogger<MessageIngestHandler>> logger;

    public MessageIngestHandlerTests()
    {
        messageRepository = new Mock<IMessageRepository>();
        queueRepository = new Mock<IQueueRepository>();
        adapterService = new Mock<IAdapterService>();
        logger = new Mock<ILogger<MessageIngestHandler>>();

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

        handler = new MessageIngestHandler(messageRepository.Object, queueRepository.Object, adapterService.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_ReturnsProcessedResult_WhenMessageIsNewAndDoesNotJoinQueue()
    {
        var commandResult = new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-1");

        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ReturnsAsync(commandResult);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TWITCH, "alice", "hello world", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        Assert.True(result.Success);
        Assert.Equal(StatusResultEnum.Processed, result.Data!.Status);
        Assert.True(result.Data.Message!.Processed);
        Assert.Same(commandResult, result.Data.CommandResult);
        messageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Once);
        adapterService.Verify(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()), Times.Once);
        queueRepository.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<QueueEntity>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdatesQueue_WhenPolicyMatches()
    {
        var joinedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var existingQueue = new QueueEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"), ProviderTypeEnum.YOUTUBE, "alice", true, joinedAt);
        List<QueueEntity> updatedQueues = [];

        queueRepository
            .Setup(r => r.GetByUserAsync(It.IsAny<string>()))
            .ReturnsAsync(existingQueue);
        queueRepository
            .Setup(r => r.UpdateAsync(It.IsAny<IEnumerable<QueueEntity>>()))
            .Callback<IEnumerable<QueueEntity>>(queues => updatedQueues.AddRange(queues))
            .ReturnsAsync(true);
        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ReturnsAsync(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-2"));

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.YOUTUBE, "alice", "!fila", DateTime.UtcNow.NormalizeToUtcMinus3()));

        Assert.True(result.Success);
        queueRepository.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<QueueEntity>>()), Times.Once);
        var updated = Assert.Single(updatedQueues);
        Assert.Equal(existingQueue.Id, updated.Id);
        Assert.Equal(existingQueue.Provider, updated.Provider);
        Assert.Equal(existingQueue.User, updated.User);
        Assert.True(updated.Selected);
        Assert.Equal(joinedAt.NormalizeToUtcMinus3(), updated.JoinedAt);
    }

    [Fact]
    public async Task Handle_ReturnsDuplicateError_WhenExistingMessageWasProcessed()
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = new ChatMessageEntity { Provider = ProviderTypeEnum.TIKTOK, Author = "alice", Text = "hello", Timestamp = timestamp, Processed = true };
        existing.EnsureIdempotencyKey();

        messageRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(existing.IdempotencyKey))
            .ReturnsAsync(existing);
        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ReturnsAsync(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-3"));

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "alice", "hello", timestamp));

        Assert.False(result.Success);
        Assert.Equal("Invalid payload", result.Error);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(StatusResultEnum.Duplicate, result.Data!.Status);
        Assert.NotNull(result.Data.Message);
        Assert.Equal(existing.Id, result.Data.Message!.Id);
        Assert.Equal(existing.IdempotencyKey, result.Data.Message.IdempotencyKey);
        adapterService.Verify(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()), Times.Never);
        queueRepository.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<QueueEntity>>()), Times.Never);
        messageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Reprocesses_WhenExistingMessageWasNotProcessed()
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = new ChatMessageEntity { Provider = ProviderTypeEnum.TIKTOK, Author = "alice", Text = "old", Timestamp = timestamp, Processed = false };
        existing.EnsureIdempotencyKey();

        messageRepository
            .Setup(r => r.GetByIdempotencyKeyAsync(existing.IdempotencyKey))
            .ReturnsAsync(existing);
        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ReturnsAsync(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-retry"));

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "alice", "new text", timestamp));

        Assert.True(result.Success);
        Assert.Equal(StatusResultEnum.Processed, result.Data!.Status);
        Assert.True(result.Data.Message!.Processed);
        Assert.Equal("new text", result.Data.Message.Text);
        messageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Never);
        messageRepository.Verify(r => r.UpdateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsProcessedStatusError_WhenSaveFails()
    {
        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ReturnsAsync(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-4"));
        messageRepository
            .Setup(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()))
            .ReturnsAsync(false);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TWITCH, "alice", "hello world", DateTime.UtcNow.NormalizeToUtcMinus3()));

        Assert.True(result.Success);
        Assert.Equal(StatusResultEnum.Error, result.Data!.Status);
        messageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenAdapterThrows()
    {
        adapterService
            .Setup(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TWITCH, "alice", "hello world", DateTime.UtcNow.NormalizeToUtcMinus3()));

        Assert.False(result.Success);
        Assert.Equal("Erro inesperado", result.Error);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        adapterService.Verify(a => a.ParseAndDispatch(It.IsAny<ChatMessageEntity>()), Times.Once);
        messageRepository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<ChatMessageEntity>>()), Times.Never);
    }

    [Fact]
    public void ToChatMessage_MapsRequestAndDefaultsTimestamp()
    {
        var request = new MessageIngestRequest(ProviderTypeEnum.TWITCH, "user", "text", null);

        var message = request.ToChatMessage();

        Assert.Equal(ProviderTypeEnum.TWITCH, message.Provider);
        Assert.Equal("user", message.Author);
        Assert.Equal("text", message.Text);
        Assert.True((DateTime.UtcNow.NormalizeToUtcMinus3() - message.Timestamp).TotalSeconds < 5);
    }

    [Fact]
    public void ToChatMessage_NormalizesAuthor_AndTimestampToUtcMinus3()
    {
        var localTimestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var request = new MessageIngestRequest(ProviderTypeEnum.TWITCH, "  user  ", "text", localTimestamp);

        var message = request.ToChatMessage();

        Assert.Equal("user", message.Author);
        Assert.Equal(localTimestamp.NormalizeToUtcMinus3(), message.Timestamp);
    }
}