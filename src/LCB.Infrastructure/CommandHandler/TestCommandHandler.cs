using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces;

namespace LCB.Infrastructure.CommandHandler;

public class TestCommandHandler : ICommandHandler
{
    public async Task<CommandDTO?> Handle(ParsedCommandDTO command)
    {
        // lógica
        var result = new CommandDTO(TypeResultEnum.Success, new("comando de teste executado", command.Args), command.Raw);

        return await Task.FromResult(result);
    }
}