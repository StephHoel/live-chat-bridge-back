using LCB.Domain.Enums;
using LCB.Domain.Extensions;

namespace LCB.Domain.Entities;

public class PointsTransactionEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public ProviderTypeEnum Provider { get; private set; }
    public string ChannelId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public long Points { get; private set; }
    public PointsTransactionSituationEnum Situation { get; private set; }
    public DateTime TransactionDateTime { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();

    public static PointsTransactionEntity Create(
        ProviderTypeEnum provider,
        string channelId,
        string userId,
        long points,
        PointsTransactionSituationEnum situation)
    {
        return new PointsTransactionEntity
        {
            Provider = provider,
            ChannelId = channelId,
            UserId = userId,
            Points = points,
            Situation = situation,
            TransactionDateTime = DateTime.UtcNow.NormalizeToUtcMinus3()
        };
    }
}
