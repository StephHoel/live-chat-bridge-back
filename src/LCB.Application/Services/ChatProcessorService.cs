using System.Threading.Channels;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Services;

public class ChatProcessorService(ChannelReader<ChatMessageModel> Reader,
                                  IServiceScopeFactory ScopeFactory,
                                  ILogger<ChatProcessorService> Logger)
{
    private const int MaxProcessingAttempts = 3;

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting {service}", nameof(ChatProcessorService));

        await foreach (var message in Reader.ReadAllAsync(cancellationToken))
        {
            var domainMessage = message.ToChatMessageEntity();

            if (!TryValidate(domainMessage, out var validationError))
            {
                LogOutcome(domainMessage, StatusResultEnum.Error, validationError);
                continue;
            }

            await ProcessWithRetryAsync(domainMessage, cancellationToken);
        }

        Logger.LogInformation("Finishing {service}", nameof(ChatProcessorService));
    }

    private async Task ProcessWithRetryAsync(ChatMessageEntity message, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxProcessingAttempts; attempt++)
        {
            var result = await ExecuteIngestUseCaseAsync(message);
            var status = result.Data?.Status ?? StatusResultEnum.Error;

            LogOutcome(message, status, result.Error);

            if (result.Success || status == StatusResultEnum.Duplicate)
                return;

            if (attempt == MaxProcessingAttempts)
            {
                Logger.LogError(
                    "Message processing failed after retries. IdempotencyKey={IdempotencyKey} Provider={Provider} DateTime={DateTime}",
                    message.IdempotencyKey,
                    message.Provider,
                    message.Timestamp);
                return;
            }

            Logger.LogWarning(
                "Retrying message processing. Attempt={Attempt} IdempotencyKey={IdempotencyKey} Provider={Provider} DateTime={DateTime}",
                attempt + 1,
                message.IdempotencyKey,
                message.Provider,
                message.Timestamp);

            await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken);
        }
    }

    private async Task<Domain.Objects.Result<MessageIngestResponse>> ExecuteIngestUseCaseAsync(ChatMessageEntity message)
    {
        using var scope = ScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<MessageIngestHandler>();

        var request = new MessageIngestRequest(message.Provider,
                                               message.Author,
                                               message.Text,
                                               message.Timestamp);

        return await handler.Handle(request);
    }

    private static bool TryValidate(ChatMessageEntity message, out string? error)
    {
        if (string.IsNullOrWhiteSpace(message.Author))
        {
            error = "Author is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            error = "Text is required";
            return false;
        }

        if (message.Timestamp == default)
        {
            error = "Timestamp is required";
            return false;
        }

        error = null;
        return true;
    }

    private void LogOutcome(ChatMessageEntity message, StatusResultEnum status, string? error)
    {
        Logger.LogInformation(
            "Message processed. IdempotencyKey={IdempotencyKey} Status={Status} Error={Error} Provider={Provider} DateTime={DateTime}",
            message.IdempotencyKey,
            status,
            error,
            message.Provider,
            message.Timestamp);
    }
}