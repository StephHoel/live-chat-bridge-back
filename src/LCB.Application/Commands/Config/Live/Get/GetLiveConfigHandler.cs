using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Constants;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Config.Live.Get;

public class GetLiveConfigHandler(
    ILiveSettingsRepository repository,
    IAuditLogService auditLogService,
    ILogger<GetLiveConfigHandler> logger)
{
    public Task<Result<LiveConfigResponse>> Handle(Guid userId, string userEmail)
        => OperationExecutor.ExecuteAsync(logger, nameof(GetLiveConfigHandler), () => ExecuteAsync(userId, userEmail));

    private async Task<Result<LiveConfigResponse>> ExecuteAsync(Guid userId, string userEmail)
    {
        var settings = await repository.GetByUserIdAsync(userId);

        if (settings is null)
        {
            settings = LiveSettingsEntity.Create(userId, userEmail);
            var created = await repository.UpsertAsync(settings);

            if (!created)
            {
                settings = await repository.GetByUserIdAsync(userId);

                if (settings is null)
                {
                    await auditLogService.WriteWithPolicyAsync(
                        userEmail,
                        AuditLogCatalog.Action.LiveSettingsViewed,
                        AuditLogCatalog.Resource.LiveSettings,
                        AuditLogStatusEnum.Failure,
                        AuditMetadataFactory.CreateEndpointOperational(
                            "GET /config/live",
                            "/config/live",
                            (int)HttpStatusCode.InternalServerError,
                            userId: userId.ToString(),
                            errorCode: "LiveConfigInitializeFailed"));

                    return Result<LiveConfigResponse>.Fail("Could not initialize live configuration", HttpStatusCode.InternalServerError);
                }
            }
        }

        await auditLogService.WriteWithPolicyAsync(
            userEmail,
            AuditLogCatalog.Action.LiveSettingsViewed,
            AuditLogCatalog.Resource.LiveSettings,
            AuditLogStatusEnum.Success,
            AuditMetadataFactory.CreateEndpointOperational(
                "GET /config/live",
                "/config/live",
                (int)HttpStatusCode.OK,
                userId: userId.ToString()));

        return Result<LiveConfigResponse>.Ok(settings.ToResponse());
    }
}
