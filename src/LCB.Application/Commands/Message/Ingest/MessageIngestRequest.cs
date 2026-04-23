using LCB.Domain.Enums;

namespace LCB.Application.Commands.Message.Ingest;

public record MessageIngestRequest(ProviderTypeEnum Provider, string Author, string Text, DateTime? Timestamp);
