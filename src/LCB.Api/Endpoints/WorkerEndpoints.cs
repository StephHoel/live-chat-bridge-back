using System.Net;
using LCB.Api.Extensions;
using LCB.Api.Security;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
using LCB.Application.Commands.Worker.Stop;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class WorkerEndpoints
{
    public static WebApplication MapWorkerEndpoints(this WebApplication app)
    {
        app.MapPost("/worker/start", async (
            HttpContext httpContext,
            WorkerStartRequest request,
            [FromServices] StartWorkerHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out var userId, out var email))
                return Result<GetWorkerStatusResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle(userId, email, request);
            return result.ToMinimalResult();
        })
        .WithTags("Worker")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.BadRequest, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.Conflict, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.ServiceUnavailable, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<GetWorkerStatusResponse>));

        app.MapPost("/worker/stop", async (
            HttpContext httpContext,
            [FromServices] StopWorkerHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out var userId, out var email))
                return Result<GetWorkerStatusResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle(userId, email);
            return result.ToMinimalResult();
        })
        .WithTags("Worker")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<GetWorkerStatusResponse>));

        app.MapGet("/worker/status", async (
            HttpContext httpContext,
            [FromServices] GetWorkerStatusHandler handler) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out var userId, out var email))
                return Result<GetWorkerStatusResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            var result = await handler.Handle(userId, email);
            return result.ToMinimalResult();
        })
        .WithTags("Worker")
        .RequireAuthorization(AuthorizationPolicies.ProtectedApi)
        .Produces((int)HttpStatusCode.OK, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<GetWorkerStatusResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<GetWorkerStatusResponse>));

        return app;
    }
}