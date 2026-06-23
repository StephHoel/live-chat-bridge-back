using LCB.Domain.Entities;

namespace LCB.Application.Commands.Message.Ingest;

public static class MessageIngestMapper
{
    public static ChatMessageEntity ToChatMessage(this MessageIngestRequest request)
    {
        var normalizedTimestamp = request.Timestamp ?? DateTime.UtcNow;
        normalizedTimestamp = normalizedTimestamp.Kind == DateTimeKind.Utc
            ? normalizedTimestamp
            : normalizedTimestamp.ToUniversalTime();

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
}