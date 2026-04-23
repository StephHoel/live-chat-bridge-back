using LCB.Domain.Enums;

namespace LCB.Domain.DTO;

public record CommandDTO(TypeResultEnum Type, PayloadDTO? Payload, string CorrelationId);

public record PayloadDTO(string Message, string[] Args);