using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Policies;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Services;

public class PointsService(
    IPointsBalanceRepository balanceRepository,
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

        var isCreated = await balanceRepository.CreditWithTransactionAsync(provider, channelId, userId, delta);
        if (!isCreated)
            logger.LogError("[PointsService] Failed to persist credit transaction (atomicity issue). Provider={Provider} ChannelId={ChannelId} UserId={UserId} Points={Points}", provider, channelId, userId, delta);
    }

    public async Task<bool> DebitAsync(ProviderTypeEnum provider, string channelId, string userId, long points)
    {
        if (points <= 0)
            return false;

        var isDebited = await balanceRepository.DebitWithTransactionAsync(provider, channelId, userId, points);

        if (!isDebited)
            logger.LogWarning("[PointsService] Debit of {Points} rejected for user {UserId}: insufficient balance, record not found, or atomicity error.", points, userId);

        return isDebited;
    }

    public async Task ClearAsync(ProviderTypeEnum provider, string channelId, string userId)
    {
        var cleared = await balanceRepository.ClearWithTransactionAsync(provider, channelId, userId);
        if (!cleared)
            logger.LogWarning("[PointsService] Clear rejected for user {UserId}: active balance record not found or atomicity error.", userId);
    }
}
