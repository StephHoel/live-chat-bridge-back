using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class PointsIntegrationTypeCatalogRepository(
    LcbDbContext context,
    ILogger<PointsIntegrationTypeCatalogRepository> logger)
    : RepositoryBase(logger), IPointsIntegrationTypeCatalogRepository
{
    public async Task<long> GetDeltaAsync(Guid streamerUserId, ProviderTypeEnum provider, IntegrationTypeEnum integrationType)
        => await ExecuteAsync(async () =>
        {
            var rule = await context.PointsIntegrationTypeCatalog
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.StreamerUserId == streamerUserId &&
                    x.Provider == provider &&
                    x.IntegrationType == integrationType);

            return rule?.Delta ?? 0;
        }, nameof(GetDeltaAsync));

    public async Task<bool> UpsertAsync(PointsIntegrationTypeCatalogEntity rule)
        => await ExecuteAsync(async () =>
        {
            var existing = await context.PointsIntegrationTypeCatalog
                .FirstOrDefaultAsync(x =>
                    x.StreamerUserId == rule.StreamerUserId &&
                    x.Provider == rule.Provider &&
                    x.IntegrationType == rule.IntegrationType);

            if (existing is null)
            {
                await context.PointsIntegrationTypeCatalog.AddAsync(rule);
                return await context.SaveChangesAsync() > 0;
            }

            existing.SetDelta(rule.Delta, rule.UpdatedBy);
            await context.SaveChangesAsync();
            return true;
        }, nameof(UpsertAsync));
}