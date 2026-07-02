namespace LCB.Domain.DTO;

public record ParsedCommandDTO(string Name, string[] Args, string Raw);