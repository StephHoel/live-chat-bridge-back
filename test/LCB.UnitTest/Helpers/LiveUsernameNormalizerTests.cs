#nullable enable

using LCB.Application.Helpers;
using Xunit;

namespace LCB.UnitTest.Helpers;

public class LiveUsernameNormalizerTests
{
    [Theory]
    [InlineData(" @alice ", "alice")]
    [InlineData("https://www.tiktok.com/@alice", "alice")]
    [InlineData("tiktok.com/@alice", "alice")]
    [InlineData("https://youtube.com/alice", "alice")]
    [InlineData("@canal", "canal")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void Normalize_ReturnsExpectedValue(string input, string? expected)
    {
        var result = LiveUsernameNormalizer.Normalize(input);
        Assert.Equal(expected, result);
    }
}
