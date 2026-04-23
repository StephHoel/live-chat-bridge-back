using System.Net;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using LCB.Infrastructure.Policies;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Message.Ingest;

public class MessageIngestHandler(IMessageRepository messageRepository, IQueueRepository queueRepository, IAdapterService adapterService, ILogger<MessageIngestHandler> logger)
{
    public async Task<Result<MessageIngestResponse>> Handle(MessageIngestRequest request)
    {
        try
        {
            logger.LogInformation("Starting ingest message by {author}", request.Author);

            var message = request.ToChatMessage();

            var existing = await messageRepository.GetByIdempotencyKeyAsync(message.IdempotencyKey);

            if (existing?.Processed == true)
                return Result<MessageIngestResponse>.Fail("Invalid payload", new(StatusResultEnum.Duplicate, existing, null), HttpStatusCode.BadRequest);

            var shouldJoinQueue = message.ShouldJoinQueue();

            if (shouldJoinQueue)
            {
                var existingQueueEntry = await queueRepository.GetByUserAsync(message.Author);

                var entry = new Queue(existingQueueEntry?.Id ?? Guid.NewGuid(),
                                      message.Provider,
                                      message.Author,
                                      existingQueueEntry?.Selected ?? false,
                                      existingQueueEntry?.JoinedAt ?? DateTime.UtcNow);

                await queueRepository.UpdateAsync([entry]);
            }

            var commandResult = await adapterService.ParseAndDispatch(message);

            message.Processed = true;

            var isMessageSaved = await messageRepository.CreateAsync([message]);

            var status = isMessageSaved ? StatusResultEnum.Processed : StatusResultEnum.Error;

            return Result<MessageIngestResponse>.Ok(new(status, message, commandResult));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unexpected | Message: {Message} | StackTrace: {Stack}", [ex.Message, ex.StackTrace]);
            return Result<MessageIngestResponse>.Fail("Error unexpected", HttpStatusCode.InternalServerError);
        }
        finally
        {
            logger.LogInformation("Finishing ingest message by {author}", request.Author);
        }
    }
}