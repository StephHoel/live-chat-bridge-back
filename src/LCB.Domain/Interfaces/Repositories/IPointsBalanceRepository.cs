using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IPointsBalanceRepository
{
    Task<PointsBalanceEntity?> GetActiveBalanceAsync(ProviderTypeEnum provider, string channelId, string userId);
    Task<PointsBalanceEntity?> UpsertAsync(ProviderTypeEnum provider, string channelId, string userId, long delta);
    /// <summary>
    /// Operação atômica: valida saldo suficiente e aplica débito em transação única.
    /// Retorna true se debit foi aplicado; false se saldo insuficiente ou registro não existe.
    /// </summary>
    Task<bool> TryDebitAsync(ProviderTypeEnum provider, string channelId, string userId, long points);
    Task<bool> ClearAsync(ProviderTypeEnum provider, string channelId, string userId);

    /// <summary>
    /// Operação completamente atômica: persiste saldo + transação de crédito no mesmo DbTransaction.
    /// Se qualquer parte falhar, ambas as operações são revertidas.
    /// </summary>
    Task<bool> CreditWithTransactionAsync(ProviderTypeEnum provider, string channelId, string userId, long delta);

    /// <summary>
    /// Operação completamente atômica: persiste saldo + transação de débito no mesmo DbTransaction.
    /// Se qualquer parte falhar, ambas as operações são revertidas.
    /// Retorna true se débito foi aplicado e transação criada; false se saldo insuficiente.
    /// </summary>
    Task<bool> DebitWithTransactionAsync(ProviderTypeEnum provider, string channelId, string userId, long points);

    /// <summary>
    /// Operação completamente atômica: persiste clear do saldo + transação de limpeza no mesmo DbTransaction.
    /// Se qualquer parte falhar, ambas as operações são revertidas.
    /// </summary>
    Task<bool> ClearWithTransactionAsync(ProviderTypeEnum provider, string channelId, string userId);
}
