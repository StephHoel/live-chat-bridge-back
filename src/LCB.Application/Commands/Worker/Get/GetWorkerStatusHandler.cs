using LCB.Application.Helpers;
using LCB.Application.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Worker.Get;

public class GetWorkerStatusHandler(
    WorkerControlService workerControlService,
    ILogger<GetWorkerStatusHandler> logger)
{
    public Task<Result<GetWorkerStatusResponse>> Handle(Guid userId, string userEmail)
        => OperationExecutor.ExecuteAsync(
            logger,
            nameof(GetWorkerStatusHandler),
            () => workerControlService.GetStatusAsync(userId, userEmail));
}