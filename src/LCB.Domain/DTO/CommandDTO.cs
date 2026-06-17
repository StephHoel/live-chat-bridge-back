using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Enums;

namespace LCB.Domain.DTO;

[ExcludeFromCodeCoverage]
public record CommandDTO(TypeResultEnum Type, PayloadDTO? Payload, string CorrelationId);