using LCB.Domain.Interfaces;

namespace LCB.Infrastructure.CommandHandler.Constants;

public static class CommandRegistry
{
    public static readonly Dictionary<string, ICommandHandler> Handlers = new()
    {
        ["!comando"] = new TestCommandHandler(),
        ["!fila"] = new FilaCommandHandler(),
    };
}