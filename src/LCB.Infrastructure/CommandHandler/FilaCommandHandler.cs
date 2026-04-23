using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces;

namespace LCB.Infrastructure.CommandHandler;

public class FilaCommandHandler : ICommandHandler
{
    public async Task<CommandDTO?> Handle(ParsedCommandDTO command)
    {
        // lógica
        var result = new CommandDTO(TypeResultEnum.Success, new("comando de fila executado", command.Args), command.Raw);

        return await Task.FromResult(result);
    }
}