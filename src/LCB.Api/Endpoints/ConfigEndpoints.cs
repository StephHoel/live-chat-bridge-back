using System.Net;
using LCB.Api.Extensions;
using LCB.Api.Security;
using LCB.Application.Commands.Config.Live;
using LCB.Application.Commands.Config.Live.Get;
using LCB.Application.Commands.Config.Live.Put;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class ConfigEndpoints
{
    public static WebApplication MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/config/live", async (HttpContext httpContext, [FromServices] GetLiveConfigHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out var userId, out var email))
                return Result<LiveConfigResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle(userId, email);
            return result.ToMinimalResult();
        })
        .WithTags("Config")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<LiveConfigResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<object?>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<LiveConfigResponse>));

        app.MapPut("/config/live", async (HttpContext httpContext, PutLiveConfigRequest request, [FromServices] PutLiveConfigHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out var userId, out var email))
                return Result<LiveConfigResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle(userId, email, request);
            return result.ToMinimalResult();
        })
        .WithTags("Config")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<LiveConfigResponse>))
        .Produces((int)HttpStatusCode.BadRequest, typeof(Result<LiveConfigResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<object?>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<LiveConfigResponse>));

        return app;
    }
}
