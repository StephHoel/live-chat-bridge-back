using System.Threading.Tasks;
using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Infrastructure.CommandHandler;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class CommandHandlersUnitTests
{
    [Fact]
    public async Task FilaHandler_ReturnsExpectedPayload()
    {
        var command = new ParsedCommandDTO("!fila", ["a", "b"], "!fila a b");

        var result = await new FilaCommandHandler().Handle(command);

        Assert.NotNull(result);
        Assert.Equal(TypeResultEnum.Success, result!.Type);
        Assert.Equal("comando de fila executado", result.Payload!.Message);
        Assert.Equal(new[] { "a", "b" }, result.Payload.Args);
        Assert.Equal(command.Raw, result.CorrelationId);
    }

    [Fact]
    public async Task TestHandler_ReturnsExpectedPayload()
    {
        var command = new ParsedCommandDTO("!comando", ["x"], "!comando x");

        var result = await new TestCommandHandler().Handle(command);

        Assert.NotNull(result);
        Assert.Equal(TypeResultEnum.Success, result!.Type);
        Assert.Equal("comando de teste executado", result.Payload!.Message);
        Assert.Equal(new[] { "x" }, result.Payload.Args);
        Assert.Equal(command.Raw, result.CorrelationId);
    }
}