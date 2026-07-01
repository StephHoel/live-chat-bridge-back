using System.Text.Json;
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

        if (string.IsNullOrWhiteSpace(resource))
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

    private bool TryValidateMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return true;

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (ContainsSensitiveContent(document.RootElement))
            {
                logger.LogWarning("Audit metadata rejected because it contains sensitive content.");
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Audit metadata rejected because it is not valid JSON.");
            return false;
        }
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
