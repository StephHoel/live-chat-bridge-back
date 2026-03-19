using System.Net;
using LCB.Domain.Objects;

namespace LCB.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToMinimalResult<T>(this Result<T> result)
    {
        if (result.Success)
            return Results.Ok(result.Data);

        return result.ErrorType switch
        {
            HttpStatusCode.BadRequest => Results.BadRequest(result),
            HttpStatusCode.NotFound => Results.NotFound(result),
            HttpStatusCode.Unauthorized => Results.Unauthorized(),
            HttpStatusCode.Forbidden => Results.Forbid(),
            HttpStatusCode.Conflict => Results.Conflict(result),
            _ => Results.InternalServerError(result)
        };
    }
}