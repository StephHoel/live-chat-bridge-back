using LCB.Domain.Entities;

namespace LCB.Application.Commands.Message.Ingest;

public static class MessageIngestMapper
{
    public static ChatMessageEntity ToChatMessage(this MessageIngestRequest request)
    {
        var message = new ChatMessageEntity
        {
            Provider = request.Provider,
            Author = request.Author,
            Text = request.Text,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
        };

        message.EnsureIdempotencyKey();

        return message;
    }
}