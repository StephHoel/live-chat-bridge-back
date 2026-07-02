using System;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Worker.Start;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.UnitTest.Helpers;
using Xunit;

namespace LCB.UnitTest.Services;

public class WorkerControlServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsInactive_ByDefault()
    {
        var (Service, UserIdA, _) = ServiceHelper.Create();

        var result = await Service.GetStatusAsync(UserIdA, "user@example.com");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(WorkerStateEnum.Inactive, result.Data!.State);
    }

    [Fact]
    public async Task StartAsync_ReturnsBadRequest_WhenNoPlatformIsEnabled()
    {
        var sut = ServiceHelper.Create(settings: null);

        var result = await sut.Service.StartAsync(sut.UserIdA, "user@example.com", new WorkerStartRequest(false, false, false));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task StartAsync_ReturnsConflict_WhenSelectedPlatformHasNoConfiguredUsername()
    {
        var settings = LiveSettingsEntity.Create(Guid.NewGuid(), "owner@example.com", null, null, null, 5);
        var (Service, UserIdA, _) = ServiceHelper.Create(settings);

        var result = await Service.StartAsync(UserIdA, "user@example.com", new WorkerStartRequest(true, false, false));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task StartAsync_ReturnsServiceUnavailable_WhenUnsupportedListenerIsSelected()
    {
        var settings = LiveSettingsEntity.Create(Guid.NewGuid(), "owner@example.com", "tiktok-user", "twitch-user", null, 5);
        var (Service, UserIdA, _) = ServiceHelper.Create(settings);

        var result = await Service.StartAsync(UserIdA, "user@example.com", new WorkerStartRequest(false, true, false));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
    }

    [Fact]
    public async Task StartAndStopAsync_TransitionsState_ForAuthenticatedUserInstance()
    {
        var settings = LiveSettingsEntity.Create(Guid.NewGuid(), "owner@example.com", "tiktok-user", null, null, 5);
        var (Service, UserIdA, _) = ServiceHelper.Create(settings);

        var started = await Service.StartAsync(UserIdA, "user@example.com", new WorkerStartRequest(true, false, false));
        var stopped = await Service.StopAsync(UserIdA, "user@example.com");

        Assert.True(started.Success);
        Assert.Equal(WorkerStateEnum.Active, started.Data!.State);
        Assert.True(stopped.Success);
        Assert.Equal(WorkerStateEnum.Inactive, stopped.Data!.State);
    }

    [Fact]
    public async Task StartAsync_IsIsolatedPerUser()
    {
        var settings = LiveSettingsEntity.Create(Guid.NewGuid(), "owner@example.com", "tiktok-user", null, null, 5);
        var (Service, UserIdA, UserIdB) = ServiceHelper.Create(settings);

        await Service.StartAsync(UserIdA, "user-a@example.com", new WorkerStartRequest(true, false, false));
        var statusUserA = await Service.GetStatusAsync(UserIdA, "user-a@example.com");
        var statusUserB = await Service.GetStatusAsync(UserIdB, "user-b@example.com");

        await Service.StopAsync(UserIdA, "user-a@example.com");

        Assert.Equal(WorkerStateEnum.Active, statusUserA.Data!.State);
        Assert.Equal(WorkerStateEnum.Inactive, statusUserB.Data!.State);
    }
}