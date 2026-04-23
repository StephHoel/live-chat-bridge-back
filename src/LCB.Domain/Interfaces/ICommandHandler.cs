using LCB.Domain.DTO;

namespace LCB.Domain.Interfaces;

public interface ICommandHandler
{
    Task<CommandDTO?> Handle(ParsedCommandDTO command);
}