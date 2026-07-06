using System.Net;
using LCB.Api.Extensions;
using LCB.Api.Security;
using LCB.Application.Commands.Queue.Get;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class QueueEndpoints
{
    public static WebApplication MapQueueEndpoints(this WebApplication app)
    {
        app.MapGet("/queue", async (
            HttpContext httpContext,
            [FromServices] GetQueueHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out _, out _))
                return Result<GetQueueResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle();
            return result.ToMinimalResult();
        })
        .WithTags("Queue")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<GetQueueResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<object?>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<GetQueueResponse>));

        return app;
    }
}