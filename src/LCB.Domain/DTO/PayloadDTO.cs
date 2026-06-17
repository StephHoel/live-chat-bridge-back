using System.Diagnostics.CodeAnalysis;

namespace LCB.Domain.DTO;

[ExcludeFromCodeCoverage]
public record PayloadDTO(string Message, string[] Args);