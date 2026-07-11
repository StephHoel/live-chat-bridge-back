using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IPointsTransactionRepository
{
    Task<bool> CreateAsync(PointsTransactionEntity transaction);
    Task<IEnumerable<PointsTransactionEntity>> GetByContextAsync(ProviderTypeEnum provider, string channelId, string userId);
}
