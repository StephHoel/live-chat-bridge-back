using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Constants;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Config.Live.Put;

public class PutLiveConfigHandler(
    ILiveSettingsRepository repository,
    IAuditLogService auditLogService,
    ILogger<PutLiveConfigHandler> logger)
{
    public Task<Result<LiveConfigResponse>> Handle(Guid userId, string userEmail, PutLiveConfigRequest request)
        => OperationExecutor.ExecuteAsync(logger, nameof(PutLiveConfigHandler), () => ExecuteAsync(userId, userEmail, request));

    private async Task<Result<LiveConfigResponse>> ExecuteAsync(Guid userId, string userEmail, PutLiveConfigRequest request)
    {
        if (request.ReloadTimeInSec.HasValue && request.ReloadTimeInSec.Value <= 0)
        {
            await auditLogService.WriteWithPolicyAsync(
                userEmail,
                AuditLogCatalog.Action.LiveSettingsUpdateFailed,
                AuditLogCatalog.Resource.LiveSettings,
                AuditLogStatusEnum.Warning,
                AuditMetadataFactory.CreateEndpointOperational(
                    "PUT /config/live",
                    "/config/live",
                    (int)HttpStatusCode.BadRequest,
                    userId: userId.ToString(),
                    errorCode: "ReloadTimeInvalid"));

            return Result<LiveConfigResponse>.Fail("ReloadTimeInSec must be greater than zero", HttpStatusCode.BadRequest);
        }

        var settings = await repository.GetByUserIdAsync(userId)
                       ?? LiveSettingsEntity.Create(userId, userEmail);

        if (request.TikTokUsername is not null)
            settings.SetTikTokUsername(LiveUsernameNormalizer.Normalize(request.TikTokUsername));

        if (request.TwitchUsername is not null)
            settings.SetTwitchUsername(LiveUsernameNormalizer.Normalize(request.TwitchUsername));

        if (request.YouTubeUsername is not null)
            settings.SetYouTubeUsername(LiveUsernameNormalizer.Normalize(request.YouTubeUsername));

        if (request.ReloadTimeInSec.HasValue)
            settings.SetReloadTimeInSec(request.ReloadTimeInSec.Value);

        settings.TouchUpdatedBy(userEmail);

        var saved = await repository.UpsertAsync(settings);
        if (!saved)
        {
            saved = await repository.UpsertAsync(settings);

            if (!saved)
            {
                await auditLogService.WriteWithPolicyAsync(
                    userEmail,
                    AuditLogCatalog.Action.LiveSettingsUpdateFailed,
                    AuditLogCatalog.Resource.LiveSettings,
                    AuditLogStatusEnum.Failure,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "PUT /config/live",
                        "/config/live",
                        (int)HttpStatusCode.InternalServerError,
                        userId: userId.ToString(),
                        errorCode: "LiveConfigSaveFailed"));

                return Result<LiveConfigResponse>.Fail("Could not save live configuration", HttpStatusCode.InternalServerError);
            }
        }

        await auditLogService.WriteWithPolicyAsync(
            userEmail,
            AuditLogCatalog.Action.LiveSettingsUpdated,
            AuditLogCatalog.Resource.LiveSettings,
            AuditLogStatusEnum.Success,
            AuditMetadataFactory.CreateEndpointOperational(
                "PUT /config/live",
                "/config/live",
                (int)HttpStatusCode.OK,
                userId: userId.ToString()));

        return Result<LiveConfigResponse>.Ok(new LiveConfigResponse
        {
            TikTokUsername = settings.TikTokUsername,
            TwitchUsername = settings.TwitchUsername,
            YouTubeUsername = settings.YouTubeUsername,
            ReloadTimeInSec = settings.ReloadTimeInSec
        });
    }
}
