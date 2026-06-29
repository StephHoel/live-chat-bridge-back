using System.Collections.Concurrent;
using System.Net;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
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

    public Task<Result<GetWorkerStatusResponse>> GetStatusAsync(Guid userId)
    {
        var session = _sessions.GetOrAdd(userId, _ => new WorkerSessionModel());
        return Task.FromResult(Result<GetWorkerStatusResponse>.Ok(session.ToResponse()));
    }

    public async Task<Result<GetWorkerStatusResponse>> StartAsync(Guid userId, string userEmail, WorkerStartRequest request)
    {
        if (!request.TikTok && !request.Twitch && !request.YouTube)
            return Result<GetWorkerStatusResponse>.Fail("At least one platform must be enabled", HttpStatusCode.BadRequest);

        var session = _sessions.GetOrAdd(userId, _ => new WorkerSessionModel());

        await session.Gate.WaitAsync();
        try
        {
            if (session.State is WorkerStateEnum.Active or WorkerStateEnum.Starting or WorkerStateEnum.Error)
                return Result<GetWorkerStatusResponse>.Ok(session.ToResponse());

            var settings = await GetLiveSettingsAsync(userId);
            if (settings is null)
                return Result<GetWorkerStatusResponse>.Fail("Live configuration not found for authenticated user", HttpStatusCode.Conflict);

            if (request.TikTok && string.IsNullOrWhiteSpace(settings.TikTokUsername))
                return Result<GetWorkerStatusResponse>.Fail("TikTok username is required to start selected listener", HttpStatusCode.Conflict);

            if (request.Twitch && string.IsNullOrWhiteSpace(settings.TwitchUsername))
                return Result<GetWorkerStatusResponse>.Fail("Twitch username is required to start selected listener", HttpStatusCode.Conflict);

            if (request.YouTube && string.IsNullOrWhiteSpace(settings.YouTubeUsername))
                return Result<GetWorkerStatusResponse>.Fail("YouTube username is required to start selected listener", HttpStatusCode.Conflict);

            if (request.Twitch || request.YouTube)
                return Result<GetWorkerStatusResponse>.Fail("Selected listener is not available in current runtime", HttpStatusCode.ServiceUnavailable);

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

            return Result<GetWorkerStatusResponse>.Ok(session.ToResponse());
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public async Task<Result<GetWorkerStatusResponse>> StopAsync(Guid userId)
    {
        var session = _sessions.GetOrAdd(userId, _ => new WorkerSessionModel());

        await session.Gate.WaitAsync();
        try
        {
            if (session.State is WorkerStateEnum.Inactive)
                return Result<GetWorkerStatusResponse>.Ok(session.ToResponse());

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
                tikTokChatProvider.Connect(username, cancellationToken);
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
}