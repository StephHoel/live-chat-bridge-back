using System.Threading.Tasks;
using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Infrastructure.CommandHandler;
using LCB.Infrastructure.CommandHandler.Constants;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class CommandHandlersTests
{
    [Fact]
    public async Task FilaAndTestHandlers_ReturnExpectedPayload()
    {
        var parsed = new ParsedCommandDTO("!fila", ["a", "b"], "!fila a b");

        var fila = await new FilaCommandHandler().Handle(parsed);
        var teste = await new TestCommandHandler().Handle(parsed);

        Assert.NotNull(fila);
        Assert.NotNull(teste);
        Assert.Equal(TypeResultEnum.Success, fila!.Type);
        Assert.Equal("comando de fila executado", fila.Payload!.Message);
        Assert.Equal(TypeResultEnum.Success, teste!.Type);
        Assert.Equal("comando de teste executado", teste.Payload!.Message);
        Assert.Equal(parsed.Raw, fila.CorrelationId);
    }

    [Fact]
    public void CommandRegistry_ContainsKnownCommands()
    {
        Assert.True(CommandRegistry.Handlers.ContainsKey("!comando"));
        Assert.True(CommandRegistry.Handlers.ContainsKey("!fila"));
        Assert.IsType<TestCommandHandler>(CommandRegistry.Handlers["!comando"]);
        Assert.IsType<FilaCommandHandler>(CommandRegistry.Handlers["!fila"]);
    }
}
