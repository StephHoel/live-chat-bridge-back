using LCB.Domain.DTO;
using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Application.Commands.Message.Ingest;

public record MessageIngestResponse(StatusResultEnum Status, ChatMessageEntity? Message, CommandDTO? CommandResult);