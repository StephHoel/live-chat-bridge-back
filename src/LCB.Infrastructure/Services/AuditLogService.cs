using System.Text.Json;
using LCB.Domain.Constants;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Services;

public class AuditLogService(
    IAuditLogRepository auditLogRepository,
    ILogger<AuditLogService> logger) : IAuditLogService
{
    private const int MaxMetadataBytes = 4096;

    private static readonly string[] SensitiveTokens =
    [
        "token",
        "password",
        "secret",
        "authorization",
        "api_key",
        "apikey",
        "jwt"
    ];

    private static readonly HashSet<string> AllowedResources =
    [
        AuditLogCatalog.Resource.WorkerControl,
        AuditLogCatalog.Resource.LiveSettings,
        AuditLogCatalog.Resource.OperationalAdmin,
        AuditLogCatalog.Resource.WorkerInbox,
        AuditLogCatalog.Resource.WorkerReplay,
        AuditLogCatalog.Resource.WorkerDeadLetter,
        AuditLogCatalog.Resource.SystemTask
    ];

    private static readonly HashSet<string> AllowedActions =
    [
        AuditLogCatalog.Action.WorkerStartRequested,
        AuditLogCatalog.Action.WorkerStartSucceeded,
        AuditLogCatalog.Action.WorkerStartFailed,
        AuditLogCatalog.Action.WorkerStopRequested,
        AuditLogCatalog.Action.WorkerStopSucceeded,
        AuditLogCatalog.Action.WorkerStopFailed,
        AuditLogCatalog.Action.WorkerStatusChecked,
        AuditLogCatalog.Action.LiveSettingsViewed,
        AuditLogCatalog.Action.LiveSettingsUpdated,
        AuditLogCatalog.Action.LiveSettingsUpdateFailed,
        AuditLogCatalog.Action.OperationalActionRequested,
        AuditLogCatalog.Action.OperationalActionSucceeded,
        AuditLogCatalog.Action.OperationalActionFailed,
        AuditLogCatalog.Action.WorkerInboxProcessingStarted,
        AuditLogCatalog.Action.WorkerInboxProcessingSucceeded,
        AuditLogCatalog.Action.WorkerInboxProcessingFailed,
        AuditLogCatalog.Action.WorkerRetryScheduled,
        AuditLogCatalog.Action.WorkerDeadLetterMoved,
        AuditLogCatalog.Action.WorkerPendingRecoveryStarted,
        AuditLogCatalog.Action.WorkerPendingRecoveryFinished,
        AuditLogCatalog.Action.SystemTaskStarted,
        AuditLogCatalog.Action.SystemTaskSucceeded,
        AuditLogCatalog.Action.SystemTaskFailed
    ];

    private static readonly HashSet<string> GlobalMetadataKeys =
    [
        "metadataVersion",
        "correlationId",
        "eventCategory",
        "occurredAtUtc"
    ];

    private static readonly HashSet<string> EndpointMetadataKeys =
    [
        "endpointName",
        "requestPath",
        "httpStatus",
        "userId",
        "errorCode"
    ];

    private static readonly HashSet<string> WorkerMetadataKeys =
    [
        "provider",
        "attempt",
        "workerState",
        "inboxMessageId",
        "errorCode"
    ];

    private static readonly HashSet<string> SystemTaskMetadataKeys =
    [
        "taskName",
        "executionId",
        "outcome",
        "errorCode"
    ];

    public async Task<bool> WriteAsync(
        string actorUser,
        string action,
        string resource,
        AuditLogStatusEnum status,
        string? metadataJson = null,
        DateTime? createdAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(actorUser))
            return false;

        if (string.IsNullOrWhiteSpace(action))
            return false;

        if (!AllowedActions.Contains(action.Trim()))
            return false;

        if (string.IsNullOrWhiteSpace(resource))
            return false;

        if (!AllowedResources.Contains(resource.Trim()))
            return false;

        if (!Enum.IsDefined(status))
            return false;

        if (!TryValidateMetadata(metadataJson))
            return false;

        var auditLog = AuditLogEntity.Create(
            actorUser.Trim(),
            action.Trim(),
            resource.Trim(),
            status,
            metadataJson,
            createdAtUtc);

        return await auditLogRepository.CreateAsync(auditLog);
    }

    public async Task<bool> WriteWithPolicyAsync(
        string actorUser,
        string action,
        string resource,
        AuditLogStatusEnum status,
        string? metadataJson = null,
        DateTime? createdAtUtc = null)
    {
        if (await WriteAsync(actorUser, action, resource, status, metadataJson, createdAtUtc))
            return true;

        if (await WriteAsync(actorUser, action, resource, status, metadataJson, createdAtUtc))
            return true;

        logger.LogError(
            "Audit write failed after retry. Action={Action} Resource={Resource} ActorUser={ActorUser}",
            action,
            resource,
            actorUser);

        return false;
    }

    private bool TryValidateMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            logger.LogWarning("Audit metadata rejected because it is missing.");
            return false;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(metadataJson) > MaxMetadataBytes)
        {
            logger.LogWarning("Audit metadata rejected because it exceeds the maximum size of {MaxBytes} bytes.", MaxMetadataBytes);
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);

            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                logger.LogWarning("Audit metadata rejected because root element is not an object.");
                return false;
            }

            if (ContainsSensitiveContent(document.RootElement))
            {
                logger.LogWarning("Audit metadata rejected because it contains sensitive content.");
                return false;
            }

            if (!MatchesContract(document.RootElement))
                return false;

            return true;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Audit metadata rejected because it is not valid JSON.");
            return false;
        }
    }

    private bool MatchesContract(JsonElement root)
    {
        if (!TryGetRequiredInt(root, "metadataVersion", out var metadataVersion) || metadataVersion != 1)
            return false;

        if (!TryGetRequiredString(root, "correlationId", out _))
            return false;

        if (!TryGetRequiredString(root, "eventCategory", out var eventCategory))
            return false;

        if (!TryGetRequiredString(root, "occurredAtUtc", out var occurredAtUtc))
            return false;

        if (!DateTimeOffset.TryParseExact(
                occurredAtUtc,
                "O",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out _))
            return false;

        var allowedForCategory = eventCategory switch
        {
            AuditLogCatalog.EventCategory.EndpointOperational => EndpointMetadataKeys,
            AuditLogCatalog.EventCategory.WorkerFlow => WorkerMetadataKeys,
            AuditLogCatalog.EventCategory.SystemTask => SystemTaskMetadataKeys,
            _ => null
        };

        if (allowedForCategory is null)
            return false;

        if (!ValidateCategoryRequiredFields(root, eventCategory))
            return false;

        var allowed = new HashSet<string>(GlobalMetadataKeys, StringComparer.Ordinal);
        foreach (var item in allowedForCategory)
            allowed.Add(item);

        return root.EnumerateObject().All(property => allowed.Contains(property.Name));
    }

    private static bool ValidateCategoryRequiredFields(JsonElement root, string eventCategory)
    {
        return eventCategory switch
        {
            AuditLogCatalog.EventCategory.EndpointOperational
                => TryGetRequiredString(root, "endpointName", out _)
                   && TryGetRequiredString(root, "requestPath", out _)
                   && TryGetRequiredInt(root, "httpStatus", out _),
            AuditLogCatalog.EventCategory.WorkerFlow
                => TryGetRequiredString(root, "provider", out _)
                   && TryGetRequiredInt(root, "attempt", out _)
                   && TryGetRequiredString(root, "workerState", out _)
                   && TryGetRequiredString(root, "inboxMessageId", out _),
            AuditLogCatalog.EventCategory.SystemTask
                => TryGetRequiredString(root, "taskName", out _)
                   && TryGetRequiredString(root, "executionId", out _)
                   && TryGetRequiredString(root, "outcome", out _),
            _ => false
        };
    }

    private static bool TryGetRequiredString(JsonElement root, string key, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(key, out var property))
            return false;

        if (property.ValueKind is not JsonValueKind.String)
            return false;

        var current = property.GetString();
        if (string.IsNullOrWhiteSpace(current))
            return false;

        value = current;
        return true;
    }

    private static bool TryGetRequiredInt(JsonElement root, string key, out int value)
    {
        value = default;
        if (!root.TryGetProperty(key, out var property))
            return false;

        if (property.ValueKind is not JsonValueKind.Number)
            return false;

        return property.TryGetInt32(out value);
    }

    private static bool ContainsSensitiveContent(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ContainsSensitiveInObject(element),
            JsonValueKind.Array => element.EnumerateArray().Any(ContainsSensitiveContent),
            JsonValueKind.String => ContainsSensitiveToken(element.GetString()),
            _ => false
        };
    }

    private static bool ContainsSensitiveInObject(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (ContainsSensitiveToken(property.Name))
                return true;

            if (ContainsSensitiveContent(property.Value))
                return true;
        }

        return false;
    }

    private static bool ContainsSensitiveToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return SensitiveTokens.Any(token =>
            value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}
