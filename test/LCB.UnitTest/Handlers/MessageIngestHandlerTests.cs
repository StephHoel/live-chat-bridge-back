using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.DTO;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class MessageIngestHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsProcessedResult_WhenMessageIsNewAndDoesNotJoinQueue()
    {
        var messageRepository = new FakeMessageRepository();
        var queueRepository = new FakeQueueRepository();
        var commandResult = new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-1");
        var adapterService = new FakeAdapterService(commandResult);
        var handler = new MessageIngestHandler(messageRepository, queueRepository, adapterService, NullLogger<MessageIngestHandler>.Instance);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TWITCH, "alice", "hello world", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        Assert.True(result.Success);
        Assert.Equal(StatusResultEnum.Processed, result.Data!.Status);
        Assert.True(result.Data.Message!.Processed);
        Assert.Same(commandResult, result.Data.CommandResult);
        Assert.Equal(1, messageRepository.CreateCalls);
        Assert.Equal(1, adapterService.Calls);
        Assert.Equal(0, queueRepository.UpdateCalls);
    }

    [Fact]
    public async Task Handle_UpdatesQueue_WhenPolicyMatches()
    {
        var joinedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var existingQueue = new QueueEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"), ProviderTypeEnum.YOUTUBE, "alice", true, joinedAt);
        var messageRepository = new FakeMessageRepository();
        var queueRepository = new FakeQueueRepository(existingQueue);
        var adapterService = new FakeAdapterService(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-2"));
        var handler = new MessageIngestHandler(messageRepository, queueRepository, adapterService, NullLogger<MessageIngestHandler>.Instance);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.YOUTUBE, "alice", "!fila", DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.Equal(1, queueRepository.UpdateCalls);
        var updated = Assert.Single(queueRepository.UpdatedQueues);
        Assert.Equal(existingQueue.Id, updated.Id);
        Assert.Equal(existingQueue.Provider, updated.Provider);
        Assert.Equal(existingQueue.User, updated.User);
        Assert.True(updated.Selected);
        Assert.Equal(joinedAt, updated.JoinedAt);
    }

    [Fact]
    public async Task Handle_ReturnsDuplicateError_WhenExistingMessageWasProcessed()
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = new ChatMessageEntity { Provider = ProviderTypeEnum.TIKTOK, Author = "alice", Text = "hello", Timestamp = timestamp, Processed = true };
        existing.EnsureIdempotencyKey();
        var messageRepository = new FakeMessageRepository(existing);
        var queueRepository = new FakeQueueRepository();
        var adapterService = new FakeAdapterService(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-3"));
        var handler = new MessageIngestHandler(messageRepository, queueRepository, adapterService, NullLogger<MessageIngestHandler>.Instance);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "alice", "hello", timestamp));

        Assert.False(result.Success);
        Assert.Equal("Invalid payload", result.Error);
        Assert.Equal(HttpStatusCode.BadRequest, result.ErrorType);
        Assert.Equal(StatusResultEnum.Duplicate, result.Data!.Status);
        Assert.Same(existing, result.Data.Message);
        Assert.Equal(0, adapterService.Calls);
        Assert.Equal(0, queueRepository.UpdateCalls);
        Assert.Equal(0, messageRepository.CreateCalls);
    }

    [Fact]
    public async Task Handle_Reprocesses_WhenExistingMessageWasNotProcessed()
    {
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var existing = new ChatMessageEntity { Provider = ProviderTypeEnum.TIKTOK, Author = "alice", Text = "old", Timestamp = timestamp, Processed = false };
        existing.EnsureIdempotencyKey();

        var messageRepository = new FakeMessageRepository(existing);
        var queueRepository = new FakeQueueRepository();
        var adapterService = new FakeAdapterService(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-retry"));
        var handler = new MessageIngestHandler(messageRepository, queueRepository, adapterService, NullLogger<MessageIngestHandler>.Instance);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "alice", "new text", timestamp));

        Assert.True(result.Success);
        Assert.Equal(StatusResultEnum.Processed, result.Data!.Status);
        Assert.True(result.Data.Message!.Processed);
        Assert.Equal("new text", result.Data.Message.Text);
        Assert.Equal(0, messageRepository.CreateCalls);
        Assert.Equal(1, messageRepository.UpdateCalls);
    }

    [Fact]
    public async Task Handle_ReturnsProcessedStatusError_WhenSaveFails()
    {
        var messageRepository = new FakeMessageRepository { CreateResult = false };
        var queueRepository = new FakeQueueRepository();
        var adapterService = new FakeAdapterService(new CommandDTO(TypeResultEnum.Success, new PayloadDTO("ok", []), "corr-4"));
        var handler = new MessageIngestHandler(messageRepository, queueRepository, adapterService, NullLogger<MessageIngestHandler>.Instance);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TWITCH, "alice", "hello world", DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.Equal(StatusResultEnum.Error, result.Data!.Status);
        Assert.Equal(1, messageRepository.CreateCalls);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenAdapterThrows()
    {
        var messageRepository = new FakeMessageRepository();
        var queueRepository = new FakeQueueRepository();
        var adapterService = new FakeAdapterService(null, throwOnParse: true);
        var handler = new MessageIngestHandler(messageRepository, queueRepository, adapterService, NullLogger<MessageIngestHandler>.Instance);

        var result = await handler.Handle(new MessageIngestRequest(ProviderTypeEnum.TWITCH, "alice", "hello world", DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Equal("Erro inesperado", result.Error);
        Assert.Equal(HttpStatusCode.InternalServerError, result.ErrorType);
        Assert.Equal(1, adapterService.Calls);
        Assert.Equal(0, messageRepository.CreateCalls);
    }

    [Fact]
    public void ToChatMessage_MapsRequestAndDefaultsTimestamp()
    {
        var request = new MessageIngestRequest(ProviderTypeEnum.TWITCH, "user", "text", null);

        var message = request.ToChatMessage();

        Assert.Equal(ProviderTypeEnum.TWITCH, message.Provider);
        Assert.Equal("user", message.Author);
        Assert.Equal("text", message.Text);
        Assert.True((DateTime.UtcNow - message.Timestamp).TotalSeconds < 5);
    }

    [Fact]
    public void ToChatMessage_NormalizesAuthor_AndTimestampToUtc()
    {
        var localTimestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var request = new MessageIngestRequest(ProviderTypeEnum.TWITCH, "  user  ", "text", localTimestamp);

        var message = request.ToChatMessage();

        Assert.Equal("user", message.Author);
        Assert.Equal(DateTimeKind.Utc, message.Timestamp.Kind);
    }

    private sealed class FakeMessageRepository(ChatMessageEntity existing = null, bool createResult = true) : IMessageRepository
    {
        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public bool CreateResult { get; set; } = createResult;

        public Task<ChatMessageEntity> GetByIdempotencyKeyAsync(string idempotencyKey)
        {
            if (existing is null)
                return Task.FromResult<ChatMessageEntity>(null);

            return Task.FromResult(existing.IdempotencyKey == idempotencyKey ? existing : null);
        }

        public Task<bool> CreateAsync(IEnumerable<ChatMessageEntity> messages)
        {
            CreateCalls++;
            return Task.FromResult(CreateResult);
        }

        public Task<IEnumerable<ChatMessageEntity>> GetAllAsync()
            => Task.FromResult<IEnumerable<ChatMessageEntity>>([]);

        public Task<IEnumerable<ChatMessageEntity>> GetByProviderAsync(ProviderTypeEnum provider)
            => Task.FromResult<IEnumerable<ChatMessageEntity>>([]);

        public Task<bool> UpdateAsync(IEnumerable<ChatMessageEntity> messages)
        {
            UpdateCalls++;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(ChatMessageEntity message)
            => Task.FromResult(false);

        public Task<bool> DeleteAllAsync()
            => Task.FromResult(true);
    }

    private sealed class FakeQueueRepository(QueueEntity existing = null) : IQueueRepository
    {
        public int UpdateCalls { get; private set; }
        public List<QueueEntity> UpdatedQueues { get; } = [];

        public Task<IEnumerable<QueueEntity>> GetAllAsync()
            => Task.FromResult<IEnumerable<QueueEntity>>([]);

        public Task<QueueEntity> GetByUserAsync(string user)
            => Task.FromResult(existing);

        public Task<bool> UpdateAsync(IEnumerable<QueueEntity> queues)
        {
            UpdateCalls++;
            UpdatedQueues.AddRange(queues);
            return Task.FromResult(true);
        }

        public Task<bool> UserExistsAsync(string user)
            => Task.FromResult(false);

        public Task<bool> DeleteAsync(QueueEntity queue)
            => Task.FromResult(false);

        public Task<bool> DeleteAllAsync()
            => Task.FromResult(true);
    }

    private sealed class FakeAdapterService(CommandDTO result, bool throwOnParse = false) : IAdapterService
    {
        public int Calls { get; private set; }

        public Task<CommandDTO> ParseAndDispatch(ChatMessageEntity message)
        {
            Calls++;

            if (throwOnParse)
                throw new InvalidOperationException("boom");

            return Task.FromResult(result);
        }
    }
}