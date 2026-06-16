using System.Text;
using LCB.Domain.Extensions;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class JwtExtensionsTests
{
    [Fact]
    public void GetBytesFromJwtKey_ReturnsNull_ForNullOrShortKey_AndBytesForValidKey()
    {
        string nullKey = null;
        var shortKey = "short";
        var validKey = new string('a', 32);

        var nullResult = nullKey.GetBytesFromJwtKey();
        var shortResult = shortKey.GetBytesFromJwtKey();
        var validResult = validKey.GetBytesFromJwtKey();

        Assert.Null(nullResult);
        Assert.Null(shortResult);
        Assert.NotNull(validResult);
        Assert.Equal(32, validResult!.Length);
        Assert.Equal(Encoding.UTF8.GetBytes(validKey), validResult);
    }
}
