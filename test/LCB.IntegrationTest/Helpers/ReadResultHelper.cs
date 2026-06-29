using LCB.Domain.Objects;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Helpers;

public static class ReadResultHelper
{
    public static async Task<Result<T>> ReadResultAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsync<Result<T>>();
        Assert.NotNull(body);

        return body!;
    }
}