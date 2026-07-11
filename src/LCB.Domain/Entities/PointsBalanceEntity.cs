using LCB.Domain.Enums;
using LCB.Domain.Extensions;

namespace LCB.Domain.Entities;

public class PointsBalanceEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public ProviderTypeEnum Provider { get; private set; }
    public string ChannelId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public long Points { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();

    public static PointsBalanceEntity Create(ProviderTypeEnum provider, string channelId, string userId, long initialPoints = 0)
    {
        return new PointsBalanceEntity
        {
            Provider = provider,
            ChannelId = channelId,
            UserId = userId,
            Points = initialPoints,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.NormalizeToUtcMinus3(),
            UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3()
        };
    }

    public void ApplyDelta(long delta)
    {
        Points = Math.Max(0, Points + delta);
        UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3();
    }
}
