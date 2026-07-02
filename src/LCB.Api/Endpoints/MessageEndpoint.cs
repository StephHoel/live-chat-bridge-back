using System.Net;
using LCB.Api.Extensions;
using LCB.Api.Security;
using LCB.Application.Commands.Message.Ingest;
using LCB.Application.Helpers;
using LCB.Domain.Constants;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Endpoints;

public static class MessageEndpoints
{
    public static WebApplication MapMessageEndpoints(this WebApplication app)
    {
        app.MapPost("/messages/ingest", async (
            HttpContext httpContext,
            MessageIngestRequest request,
            [FromServices] MessageIngestHandler handler,
            [FromServices] IAuditLogService auditLogService) =>
        {
            if (!httpContext.TryGetAuthenticatedUserData(out _, out var email))
                return Result<MessageIngestResponse>.Fail("Unauthorized", HttpStatusCode.Unauthorized).ToMinimalResult();

            await auditLogService.WriteWithPolicyAsync(
                email,
                AuditLogCatalog.Action.OperationalActionRequested,
                AuditLogCatalog.Resource.OperationalAdmin,
                AuditLogStatusEnum.Info,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /messages/ingest",
                    "/messages/ingest",
                    (int)HttpStatusCode.OK));

            var result = await handler.Handle(request, email);

            var isSuccess = result.Success;
            var action = isSuccess ? AuditLogCatalog.Action.OperationalActionSucceeded : AuditLogCatalog.Action.OperationalActionFailed;
            var status = isSuccess ? AuditLogStatusEnum.Success : AuditLogStatusEnum.Warning;

            await auditLogService.WriteWithPolicyAsync(
                email,
                action,
                AuditLogCatalog.Resource.OperationalAdmin,
                status,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /messages/ingest",
                    "/messages/ingest",
                    (int)result.StatusCode.GetValueOrDefault(HttpStatusCode.InternalServerError),
                    errorCode: result.Error));

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
