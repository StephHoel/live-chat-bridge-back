using LCB.Application.Commands.Worker.Get;
using LCB.Application.Helpers;
using LCB.Application.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Worker.Start;

public class StartWorkerHandler(
    WorkerControlService workerControlService,
    ILogger<StartWorkerHandler> logger)
{
    public Task<Result<GetWorkerStatusResponse>> Handle(Guid userId, string userEmail, WorkerStartRequest request)
        => OperationExecutor.ExecuteAsync(
            logger,
            nameof(StartWorkerHandler),
            () => workerControlService.StartAsync(userId, userEmail, request));
}