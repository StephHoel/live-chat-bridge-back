using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Domain.Models;

namespace LCB.Application.Commands.Message.Ingest;

public record MessageIngestResponse(StatusResultEnum Status, ChatMessageApiModel? Message, CommandDTO? CommandResult);