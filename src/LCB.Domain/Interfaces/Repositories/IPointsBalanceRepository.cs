using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IPointsBalanceRepository
{
    Task<PointsBalanceEntity?> GetActiveBalanceAsync(ProviderTypeEnum provider, string channelId, string userId);
    Task<PointsBalanceEntity> UpsertAsync(ProviderTypeEnum provider, string channelId, string userId, long delta);
    /// <summary>
    /// Operação atômica: valida saldo suficiente e aplica débito em transação única.
    /// Retorna true se debit foi aplicado; false se saldo insuficiente ou registro não existe.
    /// </summary>
    Task<bool> TryDebitAsync(ProviderTypeEnum provider, string channelId, string userId, long points);
    Task<bool> ClearAsync(ProviderTypeEnum provider, string channelId, string userId);
}
