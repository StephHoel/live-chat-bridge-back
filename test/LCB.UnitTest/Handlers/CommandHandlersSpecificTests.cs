using System.Threading.Tasks;
using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Infrastructure.CommandHandler;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class CommandHandlersSpecificTests
{
    [Fact]
    public async Task FilaCommandHandler_ReturnsExpectedPayload()
    {
        var parsed = new ParsedCommandDTO("!fila", ["a", "b"], "!fila a b");

        var result = await new FilaCommandHandler().Handle(parsed);

        Assert.NotNull(result);
        Assert.Equal(TypeResultEnum.Success, result!.Type);
        Assert.Equal("comando de fila executado", result.Payload!.Message);
        Assert.Equal(new[] { "a", "b" }, result.Payload.Args);
        Assert.Equal(parsed.Raw, result.CorrelationId);
    }

    [Fact]
    public async Task TestCommandHandler_ReturnsExpectedPayload()
    {
        var parsed = new ParsedCommandDTO("!comando", ["x"], "!comando x");

        var result = await new TestCommandHandler().Handle(parsed);

        Assert.NotNull(result);
        Assert.Equal(TypeResultEnum.Success, result!.Type);
        Assert.Equal("comando de teste executado", result.Payload!.Message);
        Assert.Equal(new[] { "x" }, result.Payload.Args);
        Assert.Equal(parsed.Raw, result.CorrelationId);
    }
}