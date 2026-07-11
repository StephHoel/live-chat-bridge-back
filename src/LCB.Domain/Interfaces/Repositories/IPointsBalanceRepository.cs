using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IPointsBalanceRepository
{
    Task<PointsBalanceEntity?> GetActiveBalanceAsync(ProviderTypeEnum provider, string channelId, string userId);
    Task<PointsBalanceEntity> UpsertAsync(ProviderTypeEnum provider, string channelId, string userId, long delta);
    Task<bool> ClearAsync(ProviderTypeEnum provider, string channelId, string userId);
}
