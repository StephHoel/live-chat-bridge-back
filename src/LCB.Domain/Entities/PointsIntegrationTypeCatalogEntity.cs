using LCB.Domain.Enums;
using LCB.Domain.Extensions;

namespace LCB.Domain.Entities;

public class PointsIntegrationTypeCatalogEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid StreamerUserId { get; private set; }
    public ProviderTypeEnum Provider { get; private set; }
    public IntegrationTypeEnum IntegrationType { get; private set; }
    public long Delta { get; private set; } = 0;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();
    public string? UpdatedBy { get; private set; } = null;

    public static PointsIntegrationTypeCatalogEntity Create(
        Guid streamerUserId,
        ProviderTypeEnum provider,
        IntegrationTypeEnum integrationType,
        long delta,
        string? updatedBy = null)
    {
        return new PointsIntegrationTypeCatalogEntity
        {
            StreamerUserId = streamerUserId,
            Provider = provider,
            IntegrationType = integrationType,
            Delta = Math.Max(0, delta),
            CreatedAt = DateTime.UtcNow.NormalizeToUtcMinus3(),
            UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3(),
            UpdatedBy = NormalizeUpdatedBy(updatedBy)
        };
    }

    public void SetDelta(long delta, string? updatedBy = null)
    {
        Delta = Math.Max(0, delta);
        UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3();
        UpdatedBy = NormalizeUpdatedBy(updatedBy);
    }

    private static string? NormalizeUpdatedBy(string? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            return null;

        return updatedBy.Trim();
    }
}