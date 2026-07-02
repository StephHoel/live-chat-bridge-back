using System;
using System.Threading.Tasks;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
using LCB.Application.Commands.Worker.Stop;
using LCB.Domain.Entities;
using LCB.UnitTest.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class WorkerHandlersTests
{
    [Fact]
    public async Task StartWorkerHandler_ReturnsSuccess_WhenValidRequest()
    {
        var settings = LiveSettingsEntity.Create(Guid.NewGuid(), "owner@example.com", "tiktok-user", null, null, 5);
        var (service, userId, _) = ServiceHelper.Create(settings);
        var handler = new StartWorkerHandler(service, new NullLogger<StartWorkerHandler>());

        var result = await handler.Handle(userId, "user@example.com", new WorkerStartRequest(true, false, false));

        Assert.True(result.Success);

        await service.StopAsync(userId, "user@example.com");
    }

    [Fact]
    public async Task StopWorkerHandler_ReturnsSuccess_WhenWorkerIsInactive()
    {
        var (service, userId, _) = ServiceHelper.Create();
        var handler = new StopWorkerHandler(service, new NullLogger<StopWorkerHandler>());

        var result = await handler.Handle(userId, "user@example.com");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetWorkerStatusHandler_ReturnsInactiveByDefault()
    {
        var (service, userId, _) = ServiceHelper.Create();
        var handler = new GetWorkerStatusHandler(service, new NullLogger<GetWorkerStatusHandler>());

        var result = await handler.Handle(userId, "user@example.com");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
