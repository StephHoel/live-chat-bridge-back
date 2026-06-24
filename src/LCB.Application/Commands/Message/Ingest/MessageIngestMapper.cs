using LCB.Domain.Entities;
using LCB.Domain.Extensions;
using LCB.Domain.Models;

namespace LCB.Application.Commands.Message.Ingest;

public static class MessageIngestMapper
{
    public static ChatMessageEntity ToChatMessage(this MessageIngestRequest request)
    {
        var normalizedTimestamp = (request.Timestamp ?? DateTimeExtensions.NowUtcMinus3())
            .NormalizeToUtcMinus3();

        var message = new ChatMessageEntity
        {
            Provider = request.Provider,
            Author = request.Author.Trim(),
            Text = request.Text,
            Timestamp = normalizedTimestamp,
        };

        message.EnsureIdempotencyKey();

        return message;
    }

    public static ChatMessageApiModel ToApiModel(this ChatMessageEntity message)
        => new(
            message.Id,
            message.IdempotencyKey,
            message.Provider,
            message.Author,
            message.Text,
            message.Timestamp.NormalizeToUtcMinus3(),
            message.Processed,
            message.CreatedAt,
            message.UpdatedAt
        );
}