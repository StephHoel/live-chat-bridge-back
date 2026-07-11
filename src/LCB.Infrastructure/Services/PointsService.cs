using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Policies;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Services;

public class PointsService(
    IPointsBalanceRepository balanceRepository,
    IPointsTransactionRepository transactionRepository,
    ILogger<PointsService> logger) : IPointsService
{
    public async Task<long> GetBalanceAsync(ProviderTypeEnum provider, string channelId, string userId)
    {
        var balance = await balanceRepository.GetActiveBalanceAsync(provider, channelId, userId);
        return balance?.Points ?? 0;
    }

    public async Task CreditAsync(ProviderTypeEnum provider, string channelId, string userId, IntegrationTypeEnum integrationType)
    {
        if (!PointsPolicy.IsProviderSupported(provider))
        {
            logger.LogWarning("[PointsService] Provider {Provider} not supported. Skipping credit for user {UserId}.", provider, userId);
            return;
        }

        if (!PointsPolicy.IsIntegrationTypeSupported(integrationType))
        {
            logger.LogWarning("[PointsService] IntegrationType {IntegrationType} not supported. Skipping credit for user {UserId}.", integrationType, userId);
            return;
        }

        var delta = PointsPolicy.GetDelta(provider, integrationType);

        if (delta <= 0)
            return;

        await balanceRepository.UpsertAsync(provider, channelId, userId, delta);

        var transaction = PointsTransactionEntity.Create(provider, channelId, userId, delta, PointsTransactionSituationEnum.Credit);
        var isCreated = await transactionRepository.CreateAsync(transaction);
        if (!isCreated)
            logger.LogError("[PointsService] Failed to persist points transaction. Provider={Provider} ChannelId={ChannelId} UserId={UserId} Situation={Situation} Points={Points}", provider, channelId, userId, PointsTransactionSituationEnum.Credit, delta);
    }

    public async Task<bool> DebitAsync(ProviderTypeEnum provider, string channelId, string userId, long points)
    {
        if (points <= 0)
            return false;

        var current = await GetBalanceAsync(provider, channelId, userId);

        if (current < points)
        {
            logger.LogWarning("[PointsService] Debit of {Points} rejected for user {UserId}: insufficient balance {Balance}.", points, userId, current);
            return false;
        }

        await balanceRepository.UpsertAsync(provider, channelId, userId, -points);

        var transaction = PointsTransactionEntity.Create(provider, channelId, userId, points, PointsTransactionSituationEnum.Debit);
        var isCreated = await transactionRepository.CreateAsync(transaction);
        if (!isCreated)
            logger.LogError("[PointsService] Failed to persist points transaction. Provider={Provider} ChannelId={ChannelId} UserId={UserId} Situation={Situation} Points={Points}", provider, channelId, userId, PointsTransactionSituationEnum.Debit, points);

        return true;
    }

    public async Task ClearAsync(ProviderTypeEnum provider, string channelId, string userId)
    {
        var current = await GetBalanceAsync(provider, channelId, userId);

        await balanceRepository.ClearAsync(provider, channelId, userId);

        var transaction = PointsTransactionEntity.Create(provider, channelId, userId, current, PointsTransactionSituationEnum.Clear);
        var isCreated = await transactionRepository.CreateAsync(transaction);
        if (!isCreated)
            logger.LogError("[PointsService] Failed to persist points transaction. Provider={Provider} ChannelId={ChannelId} UserId={UserId} Situation={Situation} Points={Points}", provider, channelId, userId, PointsTransactionSituationEnum.Clear, current);
    }
}
