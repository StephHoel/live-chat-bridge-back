using LCB.Domain.DTO;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.CommandHandler.Constants;

namespace LCB.Infrastructure.Services.Adapter;

public class AdapterService : IAdapterService
{
    private static ParsedCommandDTO Parser(string text)
    {
        var textTrimmed = text.Trim();

        if (string.IsNullOrEmpty(textTrimmed))
            return new("", [], text);

        var parts = textTrimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var token = parts.Length > 0 ? parts[0] : null;
        var args = parts.Skip(1).ToArray();

        if (token != null)
            token = token.StartsWith('/') ? token[1..] : token;

        return new(token, args, text);
    }

    public async Task<CommandDTO?> ParseAndDispatch(ChatMessageEntity message)
    {
        var command = Parser(message.Text);

        if (command.Name is not null && CommandRegistry.Handlers.TryGetValue(command.Name, out var handler))
            return await handler.Handle(command);

        return null;
    }
}
