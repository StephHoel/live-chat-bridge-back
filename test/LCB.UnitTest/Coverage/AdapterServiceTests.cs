using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Infrastructure.Services.Adapter;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class AdapterServiceTests
{
    [Fact]
    public async Task ParseAndDispatch_DispatchesKnownCommands_AndSupportsSlashPrefix()
    {
        var service = new AdapterService();

        var first = await service.ParseAndDispatch(new ChatMessage { Text = "!comando arg1 arg2" });
        var second = await service.ParseAndDispatch(new ChatMessage { Text = "/!fila um" });

        Assert.NotNull(first);
        Assert.Equal("comando de teste executado", first!.Payload!.Message);
        Assert.Equal(new[] { "arg1", "arg2" }, first.Payload.Args);

        Assert.NotNull(second);
        Assert.Equal("comando de fila executado", second!.Payload!.Message);
        Assert.Equal(new[] { "um" }, second.Payload!.Args);
    }

    [Fact]
    public async Task ParseAndDispatch_ReturnsNull_ForEmptyOrUnknownCommand()
    {
        var service = new AdapterService();

        var empty = await service.ParseAndDispatch(new ChatMessage { Text = "   " });
        var unknown = await service.ParseAndDispatch(new ChatMessage { Text = "!naoexiste x" });

        Assert.Null(empty);
        Assert.Null(unknown);
    }
}
