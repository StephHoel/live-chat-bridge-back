using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IPointsIntegrationTypeCatalogRepository
{
    Task<long> GetDeltaAsync(Guid streamerUserId, ProviderTypeEnum provider, IntegrationTypeEnum integrationType);
    Task<bool> UpsertAsync(PointsIntegrationTypeCatalogEntity rule);
}