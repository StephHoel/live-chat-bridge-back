using LCB.Application.Helpers;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Queue.Get;

public class GetQueueHandler(IQueueRepository repository, ILogger<GetQueueHandler> logger)
{
    public Task<Result<GetQueueResponse>> Handle()
        => OperationExecutor.ExecuteAsync(logger, nameof(GetQueueHandler), ExecuteAsync);

    private async Task<Result<GetQueueResponse>> ExecuteAsync()
    {
        var queue = await repository.GetAllAsync();

        var items = queue
            .OrderBy(x => x.CreatedAt)
            .Select(x => new QueueItemResponse(
                x.Id,
                x.Provider,
                x.User,
                x.Selected,
                x.JoinedAt,
                x.CreatedAt))
            .ToList();

        return Result<GetQueueResponse>.Ok(new(items));
    }
}