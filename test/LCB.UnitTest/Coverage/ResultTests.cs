using LCB.Domain.Objects;
using System.Net;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class ResultTests
{
    [Fact]
    public void ResultFactoryMethods_CreateExpectedShapes()
    {
        var ok = Result<int>.Ok(42);
        var fail = Result<int>.Fail("erro", HttpStatusCode.BadRequest);
        var failWithData = Result<int>.Fail("erro2", 7, HttpStatusCode.Conflict);

        Assert.True(ok.Success);
        Assert.Equal(42, ok.Data);
        Assert.Null(ok.Error);

        Assert.False(fail.Success);
        Assert.Equal("erro", fail.Error);
        Assert.Equal(HttpStatusCode.BadRequest, fail.ErrorType);
        Assert.Equal(default, fail.Data);

        Assert.False(failWithData.Success);
        Assert.Equal("erro2", failWithData.Error);
        Assert.Equal(7, failWithData.Data);
        Assert.Equal(HttpStatusCode.Conflict, failWithData.ErrorType);
    }
}
