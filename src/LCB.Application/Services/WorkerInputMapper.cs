using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Extensions;
using LCB.Domain.Models;

namespace LCB.Application.Services;

public static class WorkerInputMapper
{
    public static ChatMessageEntity ToChatMessageEntity(this ChatMessageModel workerInput)
    {
        var provider = Enum.TryParse<ProviderTypeEnum>(workerInput.Platform, true, out var parsed)
            ? parsed
            : ProviderTypeEnum.TIKTOK;

        var message = new ChatMessageEntity
        {
            Provider = provider,
            Author = workerInput.User.Trim(),
            Text = workerInput.Text,
            Timestamp = workerInput.CreatedAt.NormalizeToUtcMinus3(),
        };

        message.EnsureIdempotencyKey();
        return message;
    }
}