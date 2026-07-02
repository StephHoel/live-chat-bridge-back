using System.Text.Json;
using LCB.Domain.Constants;

namespace LCB.Application.Helpers;

public static class AuditMetadataFactory
{
    private const int MetadataVersion = 1;

    public static string CreateEndpointOperational(
        string endpointName,
        string requestPath,
        int httpStatus,
        string? correlationId = null,
        string? userId = null,
        string? errorCode = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["metadataVersion"] = MetadataVersion,
            ["correlationId"] = correlationId ?? Guid.NewGuid().ToString("N"),
            ["eventCategory"] = AuditLogCatalog.EventCategory.EndpointOperational,
            ["occurredAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["endpointName"] = endpointName,
            ["requestPath"] = requestPath,
            ["httpStatus"] = httpStatus,
            ["userId"] = userId,
            ["errorCode"] = errorCode
        };

        return Serialize(payload);
    }

    public static string CreateWorkerFlow(
        string provider,
        int attempt,
        string workerState,
        string inboxMessageId,
        string? correlationId = null,
        string? errorCode = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["metadataVersion"] = MetadataVersion,
            ["correlationId"] = correlationId ?? Guid.NewGuid().ToString("N"),
            ["eventCategory"] = AuditLogCatalog.EventCategory.WorkerFlow,
            ["occurredAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["provider"] = provider,
            ["attempt"] = attempt,
            ["workerState"] = workerState,
            ["inboxMessageId"] = inboxMessageId,
            ["errorCode"] = errorCode
        };

        return Serialize(payload);
    }

    public static string CreateSystemTask(
        string taskName,
        string executionId,
        string outcome,
        string? correlationId = null,
        string? errorCode = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["metadataVersion"] = MetadataVersion,
            ["correlationId"] = correlationId ?? Guid.NewGuid().ToString("N"),
            ["eventCategory"] = AuditLogCatalog.EventCategory.SystemTask,
            ["occurredAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["taskName"] = taskName,
            ["executionId"] = executionId,
            ["outcome"] = outcome,
            ["errorCode"] = errorCode
        };

        return Serialize(payload);
    }

    private static string Serialize(Dictionary<string, object?> payload)
        => JsonSerializer.Serialize(payload.Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value));
}
