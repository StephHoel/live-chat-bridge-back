using System.Net;
using LCB.Domain.Objects;

namespace LCB.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToMinimalResult<T>(this Result<T> result)
    {
        return result.StatusCode switch
        {
            HttpStatusCode.OK => Results.Ok(result),
            HttpStatusCode.Created => Results.Created(string.Empty, result),
            HttpStatusCode.BadRequest => Results.BadRequest(result),
            HttpStatusCode.NotFound => Results.NotFound(result),
            HttpStatusCode.Unauthorized => Results.Json(result, statusCode: (int)HttpStatusCode.Unauthorized),
            HttpStatusCode.Forbidden => Results.Json(result, statusCode: (int)HttpStatusCode.Forbidden),
            HttpStatusCode.Conflict => Results.Conflict(result),
            HttpStatusCode.ServiceUnavailable => Results.Json(result, statusCode: (int)HttpStatusCode.ServiceUnavailable),
            _ => Results.InternalServerError(result)
        };
    }
}