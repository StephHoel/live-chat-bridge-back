using System.Collections.Concurrent;
using System.Net;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
using LCB.Application.Helpers;
using LCB.Domain.Constants;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Models;
using LCB.Domain.Objects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Services;

public class WorkerControlService(
    IServiceScopeFactory scopeFactory,
    ITikTokChatProvider tikTokChatProvider,
    ILogger<WorkerControlService> logger)
{
    private readonly ConcurrentDictionary<Guid, WorkerSessionModel> _sessions = new();

    public async Task<Result<GetWorkerStatusResponse>> GetStatusAsync(Guid userId, string userEmail)
    {
        var session = _sessions.GetOrAdd(userId, _ => new WorkerSessionModel());
        var result = Result<GetWorkerStatusResponse>.Ok(session.ToResponse());

        await WriteAuditAsync(
            userEmail,
            AuditLogCatalog.Action.WorkerStatusChecked,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Info,
            AuditMetadataFactory.CreateEndpointOperational(
                "GET /worker/status",
                "/worker/status",
                (int)result.StatusCode.GetValueOrDefault(HttpStatusCode.OK),
                userId: userId.ToString()));

        return result;
    }

    public async Task<Result<GetWorkerStatusResponse>> StartAsync(Guid userId, string userEmail, WorkerStartRequest request)
    {
        await WriteAuditAsync(
            userEmail,
            AuditLogCatalog.Action.WorkerStartRequested,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Info,
            AuditMetadataFactory.CreateEndpointOperational(
                "POST /worker/start",
                "/worker/start",
                (int)HttpStatusCode.OK,
                userId: userId.ToString()));

        if (!request.TikTok && !request.Twitch && !request.YouTube)
        {
            var failed = Result<GetWorkerStatusResponse>.Fail("At least one platform must be enabled", HttpStatusCode.BadRequest);

            await WriteAuditAsync(
                userEmail,
                AuditLogCatalog.Action.WorkerStartFailed,
                AuditLogCatalog.Resource.WorkerControl,
                AuditLogStatusEnum.Warning,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /worker/start",
                    "/worker/start",
                    (int)failed.StatusCode.GetValueOrDefault(HttpStatusCode.BadRequest),
                    userId: userId.ToString(),
                    errorCode: "NoPlatformEnabled"));

            return Result<GetWorkerStatusResponse>.Fail("At least one platform must be enabled", HttpStatusCode.BadRequest);
        }

        var session = _sessions.GetOrAdd(userId, _ => new WorkerSessionModel());

        await session.Gate.WaitAsync();
        try
        {
            if (session.State is WorkerStateEnum.Active or WorkerStateEnum.Starting or WorkerStateEnum.Error)
                return Result<GetWorkerStatusResponse>.Ok(session.ToResponse());

            var settings = await GetLiveSettingsAsync(userId);
            if (settings is null)
            {
                var failed = Result<GetWorkerStatusResponse>.Fail("Live configuration not found for authenticated user", HttpStatusCode.Conflict);
                await WriteAuditAsync(
                    userEmail,
                    AuditLogCatalog.Action.WorkerStartFailed,
                    AuditLogCatalog.Resource.WorkerControl,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /worker/start",
                        "/worker/start",
                        (int)failed.StatusCode.GetValueOrDefault(HttpStatusCode.Conflict),
                        userId: userId.ToString(),
                        errorCode: "LiveConfigNotFound"));

                return Result<GetWorkerStatusResponse>.Fail("Live configuration not found for authenticated user", HttpStatusCode.Conflict);
            }

            if (request.TikTok && string.IsNullOrWhiteSpace(settings.TikTokUsername))
            {
                var failed = Result<GetWorkerStatusResponse>.Fail("TikTok username is required to start selected listener", HttpStatusCode.Conflict);
                await WriteAuditAsync(
                    userEmail,
                    AuditLogCatalog.Action.WorkerStartFailed,
                    AuditLogCatalog.Resource.WorkerControl,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /worker/start",
                        "/worker/start",
                        (int)failed.StatusCode.GetValueOrDefault(HttpStatusCode.Conflict),
                        userId: userId.ToString(),
                        errorCode: "TikTokUsernameMissing"));

                return Result<GetWorkerStatusResponse>.Fail("TikTok username is required to start selected listener", HttpStatusCode.Conflict);
            }

            if (request.Twitch && string.IsNullOrWhiteSpace(settings.TwitchUsername))
            {
                var failed = Result<GetWorkerStatusResponse>.Fail("Twitch username is required to start selected listener", HttpStatusCode.Conflict);
                await WriteAuditAsync(
                    userEmail,
                    AuditLogCatalog.Action.WorkerStartFailed,
                    AuditLogCatalog.Resource.WorkerControl,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /worker/start",
                        "/worker/start",
                        (int)failed.StatusCode.GetValueOrDefault(HttpStatusCode.Conflict),
                        userId: userId.ToString(),
                        errorCode: "TwitchUsernameMissing"));

                return Result<GetWorkerStatusResponse>.Fail("Twitch username is required to start selected listener", HttpStatusCode.Conflict);
            }

            if (request.YouTube && string.IsNullOrWhiteSpace(settings.YouTubeUsername))
            {
                var failed = Result<GetWorkerStatusResponse>.Fail("YouTube username is required to start selected listener", HttpStatusCode.Conflict);
                await WriteAuditAsync(
                    userEmail,
                    AuditLogCatalog.Action.WorkerStartFailed,
                    AuditLogCatalog.Resource.WorkerControl,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /worker/start",
                        "/worker/start",
                        (int)failed.StatusCode.GetValueOrDefault(HttpStatusCode.Conflict),
                        userId: userId.ToString(),
                        errorCode: "YouTubeUsernameMissing"));

                return Result<GetWorkerStatusResponse>.Fail("YouTube username is required to start selected listener", HttpStatusCode.Conflict);
            }

            if (request.Twitch || request.YouTube)
            {
                var failed = Result<GetWorkerStatusResponse>.Fail("Selected listener is not available in current runtime", HttpStatusCode.ServiceUnavailable);
                await WriteAuditAsync(
                    userEmail,
                    AuditLogCatalog.Action.WorkerStartFailed,
                    AuditLogCatalog.Resource.WorkerControl,
                    AuditLogStatusEnum.Warning,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /worker/start",
                        "/worker/start",
                        (int)failed.StatusCode.GetValueOrDefault(HttpStatusCode.ServiceUnavailable),
                        userId: userId.ToString(),
                        errorCode: "UnsupportedListener"));

                return Result<GetWorkerStatusResponse>.Fail("Selected listener is not available in current runtime", HttpStatusCode.ServiceUnavailable);
            }

            session.State = WorkerStateEnum.Starting;
            session.TikTok = request.TikTok;
            session.Twitch = request.Twitch;
            session.YouTube = request.YouTube;

            var cancellationSource = new CancellationTokenSource();
            session.CancellationTokenSource = cancellationSource;

            if (request.TikTok)
            {
                var username = settings.TikTokUsername!.Trim().TrimStart('@');
                session.TikTokTask = Task.Run(() => RunTikTokListenerLoopAsync(userId, userEmail, username, cancellationSource.Token), cancellationSource.Token);
            }

            session.State = WorkerStateEnum.Active;

            await WriteAuditAsync(
                userEmail,
                AuditLogCatalog.Action.WorkerStartSucceeded,
                AuditLogCatalog.Resource.WorkerControl,
                AuditLogStatusEnum.Success,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /worker/start",
                    "/worker/start",
                    (int)HttpStatusCode.OK,
                    userId: userId.ToString()));

            return Result<GetWorkerStatusResponse>.Ok(session.ToResponse());
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<Result<GetWorkerStatusResponse>> StopAsync(Guid userId, string userEmail)
    {
        await WriteAuditAsync(
            userEmail,
            AuditLogCatalog.Action.WorkerStopRequested,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Info,
            AuditMetadataFactory.CreateEndpointOperational(
                "POST /worker/stop",
                "/worker/stop",
                (int)HttpStatusCode.OK,
                userId: userId.ToString()));

        var session = _sessions.GetOrAdd(userId, _ => new WorkerSessionModel());

        await session.Gate.WaitAsync();
        try
        {
            if (session.State is WorkerStateEnum.Inactive)
            {
                var alreadyStopped = Result<GetWorkerStatusResponse>.Ok(session.ToResponse());

                await WriteAuditAsync(
                    userEmail,
                    AuditLogCatalog.Action.WorkerStopSucceeded,
                    AuditLogCatalog.Resource.WorkerControl,
                    AuditLogStatusEnum.Info,
                    AuditMetadataFactory.CreateEndpointOperational(
                        "POST /worker/stop",
                        "/worker/stop",
                        (int)alreadyStopped.StatusCode.GetValueOrDefault(HttpStatusCode.OK),
                        userId: userId.ToString()));

                return alreadyStopped;
            }

            session.State = WorkerStateEnum.Stopping;

            var cancellationSource = session.CancellationTokenSource;
            var tikTokTask = session.TikTokTask;

            session.CancellationTokenSource = null;
            session.TikTokTask = null;
            session.TikTok = false;
            session.Twitch = false;
            session.YouTube = false;

            cancellationSource?.Cancel();

            if (tikTokTask is not null)
            {
                try
                {
                    await tikTokTask;
                }
                catch (OperationCanceledException)
                {
                    // expected while stopping
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Listener task faulted while stopping. UserId={UserId}", userId);
                }
            }

            cancellationSource?.Dispose();

            session.State = WorkerStateEnum.Inactive;

            await WriteAuditAsync(
                userEmail,
                AuditLogCatalog.Action.WorkerStopSucceeded,
                AuditLogCatalog.Resource.WorkerControl,
                AuditLogStatusEnum.Success,
                AuditMetadataFactory.CreateEndpointOperational(
                    "POST /worker/stop",
                    "/worker/stop",
                    (int)HttpStatusCode.OK,
                    userId: userId.ToString()));

            return Result<GetWorkerStatusResponse>.Ok(session.ToResponse());
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private async Task RunTikTokListenerLoopAsync(Guid userId, string userEmail, string username, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                tikTokChatProvider.Connect(username, userEmail, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error while running TikTok listener. UserId={UserId} UserEmail={UserEmail}",
                    userId,
                    userEmail);

                MarkSessionAsError(userId);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void MarkSessionAsError(Guid userId)
    {
        if (!_sessions.TryGetValue(userId, out var session))
            return;

        session.Gate.Wait();

        try
        {
            if (session.State is WorkerStateEnum.Active or WorkerStateEnum.Starting)
                session.State = WorkerStateEnum.Error;
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private async Task<LiveSettingsEntity?> GetLiveSettingsAsync(Guid userId)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILiveSettingsRepository>();
        return await repository.GetByUserIdAsync(userId);
    }

    private async Task WriteAuditAsync(
        string actorUser,
        string action,
        string resource,
        AuditLogStatusEnum status,
        string metadataJson)
    {
        using var scope = scopeFactory.CreateScope();
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        await auditLogService.WriteWithPolicyAsync(
            actorUser,
            action,
            resource,
            status,
            metadataJson);
    }
}