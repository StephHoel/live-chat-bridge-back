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

    public async Task<PointsBalanceEntity?> UpsertAsync(ProviderTypeEnum provider, string channelId, string userId, long delta)
        => await ExecuteAsync(async () =>
        {
            var now = Domain.Extensions.DateTimeExtensions.NormalizeToUtcMinus3(DateTime.UtcNow);

            var updated = await context.PointsBalances
                 .Where(x =>
                    x.Provider == provider &&
                    x.ChannelId == channelId &&
                    x.UserId == userId &&
                    x.IsActive)
                 .ExecuteUpdateAsync(setters => setters
                     .SetProperty(x => x.Points, x => x.Points + delta < 0 ? 0 : x.Points + delta)
                     .SetProperty(x => x.UpdatedAt, now));

            if (updated > 0)
            {
                return await context.PointsBalances
                     .AsNoTracking()
                     .FirstAsync(x =>
                         x.Provider == provider &&
                         x.ChannelId == channelId &&
                         x.UserId == userId &&
                         x.IsActive);
            }

            var balance = PointsBalanceEntity.Create(provider, channelId, userId, Math.Max(0, delta));
            try
            {
                await context.PointsBalances.AddAsync(balance);
                await context.SaveChangesAsync();
                return balance;
            }
            catch (DbUpdateException)
            {
                await context.PointsBalances
                    .Where(x =>
                        x.Provider == provider &&
                        x.ChannelId == channelId &&
                        x.UserId == userId &&
                        x.IsActive)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Points, x => x.Points + delta < 0 ? 0 : x.Points + delta)
                        .SetProperty(x => x.UpdatedAt, now));

                return await context.PointsBalances
                         .AsNoTracking()
                         .FirstAsync(x =>
                             x.Provider == provider &&
                             x.ChannelId == channelId &&
                             x.UserId == userId &&
                             x.IsActive);
            }
        }, nameof(UpsertAsync));

    public async Task<bool> TryDebitAsync(ProviderTypeEnum provider, string channelId, string userId, long points)
        => await ExecuteAsync(async () =>
        {
            if (points <= 0)
                return false;

            var now = Domain.Extensions.DateTimeExtensions.NormalizeToUtcMinus3(DateTime.UtcNow);

            var affected = await context.PointsBalances
                 .Where(x => x.Provider == provider
                             && x.ChannelId == channelId
                             && x.UserId == userId
                             && x.IsActive
                             && x.Points >= points)
                 .ExecuteUpdateAsync(setters => setters
                     .SetProperty(
                        x => x.Points,
                        x => x.Points - points)
                     .SetProperty(x => x.UpdatedAt, now));

            return affected > 0;
        }, nameof(TryDebitAsync));

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
