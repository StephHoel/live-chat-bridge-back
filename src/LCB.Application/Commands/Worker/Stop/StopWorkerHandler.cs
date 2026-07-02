using LCB.Application.Commands.Worker.Get;
using LCB.Application.Helpers;
using LCB.Application.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Worker.Stop;

public class StopWorkerHandler(
    WorkerControlService workerControlService,
    ILogger<StopWorkerHandler> logger)
{
    public Task<Result<GetWorkerStatusResponse>> Handle(Guid userId, string userEmail)
        => OperationExecutor.ExecuteAsync(
            logger,
            nameof(StopWorkerHandler),
            () => workerControlService.StopAsync(userId, userEmail));
}