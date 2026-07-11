using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class PointsBalanceRepository(
    LcbDbContext context,
    ILogger<PointsBalanceRepository> logger)
    : RepositoryBase(logger), IPointsBalanceRepository
{
    public async Task<PointsBalanceEntity?> GetActiveBalanceAsync(ProviderTypeEnum provider, string channelId, string userId)
        => await ExecuteAsync(async () =>
        {
            return await context.PointsBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Provider == provider &&
                    x.ChannelId == channelId &&
                    x.UserId == userId &&
                    x.IsActive);
        }, nameof(GetActiveBalanceAsync));

    public async Task<PointsBalanceEntity> UpsertAsync(ProviderTypeEnum provider, string channelId, string userId, long delta)
        => await ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var balance = await context.PointsBalances
                .FirstOrDefaultAsync(x =>
                    x.Provider == provider &&
                    x.ChannelId == channelId &&
                    x.UserId == userId &&
                    x.IsActive);

            if (balance is null)
            {
                balance = PointsBalanceEntity.Create(provider, channelId, userId, Math.Max(0, delta));
                await context.PointsBalances.AddAsync(balance);
            }
            else
            {
                balance.ApplyDelta(delta);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return balance;
        }, nameof(UpsertAsync));

    public async Task<bool> ClearAsync(ProviderTypeEnum provider, string channelId, string userId)
        => await ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var balance = await context.PointsBalances
                .FirstOrDefaultAsync(x =>
                    x.Provider == provider &&
                    x.ChannelId == channelId &&
                    x.UserId == userId &&
                    x.IsActive);

            if (balance is null)
                return false;

            balance.Deactivate();
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }, nameof(ClearAsync));
}
