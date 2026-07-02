using System.Net;
using System.Text.Json;
using LCB.Api.Extensions;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Recover;
using LCB.Application.Commands.Register;
using LCB.Application.Helpers;
using LCB.Domain.Constants;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, [FromServices] LoginHandler handler) =>
        {
            var result = await handler.Handle(request);
            return result.ToMinimalResult();
        })
        .WithTags("Auth")
        .AllowAnonymous()
        .Produces((int)HttpStatusCode.OK, typeof(Result<LoginResponse>))
        .Produces((int)HttpStatusCode.Unauthorized, typeof(Result<object?>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<LoginResponse>));

        app.MapPost("/auth/register", async (RegisterRequest request, [FromServices] RegisterHandler handler) =>
        {
            var result = await handler.Handle(request);
            return result.ToMinimalResult();
        })
        .WithTags("Auth")
        .AllowAnonymous()
        .Produces((int)HttpStatusCode.Created, typeof(Result<RegisterResponse>))
        .Produces((int)HttpStatusCode.BadRequest, typeof(Result<RegisterResponse>))
        .Produces((int)HttpStatusCode.Conflict, typeof(Result<RegisterResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<RegisterResponse>));

        app.MapPost("/auth/recover/", async (
            HttpContext httpContext,
            [FromServices] RecoverHandler handler,
            [FromServices] IAuditLogService auditLogService) =>
        {
            const string actorUser = "system:recover";

            await auditLogService.WriteWithPolicyAsync(
                actorUser,
                AuditLogCatalog.Action.OperationalActionRequested,
                AuditLogCatalog.Resource.OperationalAdmin,
                AuditLogStatusEnum.Info,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /auth/recover/",
                    "/auth/recover/",
                    (int)HttpStatusCode.OK));

            RecoverRequest? request;
            try
            {
                request = await httpContext.Request.ReadFromJsonAsync<RecoverRequest>(cancellationToken: httpContext.RequestAborted);
            }
            catch (JsonException)
            {
                var invalidPayload = Result<RecoverResponse>.Fail("Invalid payload", HttpStatusCode.UnprocessableEntity);

                await auditLogService.WriteWithPolicyAsync(
                    actorUser,
                    AuditLogCatalog.Action.OperationalActionFailed,
                    AuditLogCatalog.Resource.OperationalAdmin,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /auth/recover/",
                        "/auth/recover/",
                        (int)HttpStatusCode.UnprocessableEntity,
                        errorCode: invalidPayload.Error));

                return invalidPayload.ToMinimalResult();
            }

            if (request is null)
            {
                var emptyPayload = Result<RecoverResponse>.Fail("Invalid payload", HttpStatusCode.UnprocessableEntity);

                await auditLogService.WriteWithPolicyAsync(
                    actorUser,
                    AuditLogCatalog.Action.OperationalActionFailed,
                    AuditLogCatalog.Resource.OperationalAdmin,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /auth/recover/",
                        "/auth/recover/",
                        (int)HttpStatusCode.UnprocessableEntity,
                        errorCode: emptyPayload.Error));

                return emptyPayload.ToMinimalResult();
            }

            var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await handler.Handle(request, remoteIpAddress);

            var isSuccess = result.Success;
            var action = isSuccess ? AuditLogCatalog.Action.OperationalActionSucceeded : AuditLogCatalog.Action.OperationalActionFailed;
            var status = isSuccess ? AuditLogStatusEnum.Success : AuditLogStatusEnum.Warning;

            await auditLogService.WriteWithPolicyAsync(
                actorUser,
                action,
                AuditLogCatalog.Resource.OperationalAdmin,
                status,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /auth/recover/",
                    "/auth/recover/",
                    (int)result.StatusCode.GetValueOrDefault(HttpStatusCode.InternalServerError),
                    errorCode: result.Error));

            return result.ToMinimalResult();
        })
        .WithTags("Auth")
        .AllowAnonymous()
        .Produces((int)HttpStatusCode.OK, typeof(Result<RecoverResponse>))
        .Produces((int)HttpStatusCode.UnprocessableEntity, typeof(Result<RecoverResponse>))
        .Produces((int)HttpStatusCode.TooManyRequests, typeof(Result<RecoverResponse>))
        .Produces((int)HttpStatusCode.InternalServerError, typeof(Result<RecoverResponse>));

        return app;
    }
}
