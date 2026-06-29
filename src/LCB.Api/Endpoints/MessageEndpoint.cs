using System.Net;
using LCB.Api.Security;
using LCB.Api.Extensions;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class MessageEndpoints
{
    public static WebApplication MapMessageEndpoints(this WebApplication app)
    {
        app.MapPost("/messages/ingest", async (HttpContext httpContext, MessageIngestRequest request, [FromServices] MessageIngestHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out _, out var email))
                return Result<MessageIngestResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle(request, email);
            return result.ToMinimalResult();
        })
        .WithTags("Messages")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<MessageIngestResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<object?>))
        .Produces((int)HttpStatusCode.BadRequest, typeof(Result<MessageIngestResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<MessageIngestResponse>));

        return app;
    }
}
