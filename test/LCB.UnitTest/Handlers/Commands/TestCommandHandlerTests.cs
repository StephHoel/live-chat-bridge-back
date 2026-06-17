using System.Threading.Tasks;
using LCB.Domain.DTO;
using LCB.Domain.Enums;
using LCB.Infrastructure.CommandHandler;
using Xunit;

namespace LCB.UnitTest.Handlers.Commands;

public class TestCommandHandlerTests
{
    [Fact]
    public async Task TestHandler_ReturnsExpectedPayload()
    {
        string[] inputParameters = ["x"];
        var command = new ParsedCommandDTO("!comando", inputParameters, "!comando x");

        var result = await new TestCommandHandler().Handle(command);

        Assert.NotNull(result);
        Assert.Equal(TypeResultEnum.Success, result!.Type);
        Assert.Equal("comando de teste executado", result.Payload!.Message);
        Assert.Equal(inputParameters, result.Payload.Args);
        Assert.Equal(command.Raw, result.CorrelationId);
    }
}