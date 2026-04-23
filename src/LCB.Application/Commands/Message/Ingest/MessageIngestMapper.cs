using LCB.Domain.Entities;

namespace LCB.Application.Commands.Message.Ingest;

public static class MessageIngestMapper
{
    public static ChatMessage ToChatMessage(this MessageIngestRequest request)
    {
        return new()
        {
            Provider = request.Provider,
            Author = request.Author,
            Text = request.Text,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
        };
    }
}