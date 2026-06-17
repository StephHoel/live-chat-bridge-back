using System.Threading.Tasks;
using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Infrastructure.CommandHandler;
using Xunit;

namespace LCB.UnitTest.Handlers.Commands;

public class FilaCommandHandlerTests
{
    [Fact]
    public async Task FilaHandler_ReturnsExpectedPayload()
    {
        string[] inputParameters = ["a", "b"];
        var command = new ParsedCommandDTO("!fila", inputParameters, "!fila a b");

        var result = await new FilaCommandHandler().Handle(command);

        Assert.NotNull(result);
        Assert.Equal(TypeResultEnum.Success, result!.Type);
        Assert.Equal("comando de fila executado", result.Payload!.Message);
        Assert.Equal(inputParameters, result.Payload.Args);
        Assert.Equal(command.Raw, result.CorrelationId);
    }
}