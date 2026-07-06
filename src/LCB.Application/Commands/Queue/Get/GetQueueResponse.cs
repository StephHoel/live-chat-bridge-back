using LCB.Domain.Enums;

namespace LCB.Application.Commands.Queue.Get;

public record QueueItemResponse(
    Guid Id,
    ProviderTypeEnum Provider,
    string User,
    bool Selected,
    DateTime JoinedAt,
    DateTime CreatedAt);

public record GetQueueResponse(IReadOnlyList<QueueItemResponse> Items);