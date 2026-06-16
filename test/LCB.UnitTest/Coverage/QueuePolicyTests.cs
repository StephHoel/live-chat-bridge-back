using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Infrastructure.Policies;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class QueuePolicyTests
{
    [Theory]
    [InlineData(ProviderTypeEnum.TIKTOK, "fila", true)]
    [InlineData(ProviderTypeEnum.TIKTOK, " !fila ", true)]
    [InlineData(ProviderTypeEnum.TIKTOK, "outra", false)]
    [InlineData(ProviderTypeEnum.YOUTUBE, "/fila", true)]
    [InlineData(ProviderTypeEnum.YOUTUBE, "!fila", true)]
    [InlineData(ProviderTypeEnum.YOUTUBE, "fila", false)]
    [InlineData(ProviderTypeEnum.TWITCH, "!fila", true)]
    [InlineData(ProviderTypeEnum.TWITCH, "fila", false)]
    public void ShouldJoinQueue_RespectsProviderRules(ProviderTypeEnum provider, string text, bool expected)
    {
        var message = new ChatMessage { Provider = provider, Text = text };

        Assert.Equal(expected, message.ShouldJoinQueue());
    }

    [Fact]
    public void ShouldJoinQueue_ReturnsFalse_ForUnknownProvider()
    {
        var message = new ChatMessage { Provider = (ProviderTypeEnum)999, Text = "!fila" };

        Assert.False(message.ShouldJoinQueue());
    }
}
