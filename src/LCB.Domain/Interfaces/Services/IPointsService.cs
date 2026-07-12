using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Services;

public interface IPointsService
{
    Task<long> GetBalanceAsync(ProviderTypeEnum provider, string channelId, string userId);
    Task CreditAsync(Guid streamerUserId, ProviderTypeEnum provider, string channelId, string userId, IntegrationTypeEnum integrationType);
    Task<bool> DebitAsync(ProviderTypeEnum provider, string channelId, string userId, long points);
    Task ClearAsync(ProviderTypeEnum provider, string channelId, string userId);
}
