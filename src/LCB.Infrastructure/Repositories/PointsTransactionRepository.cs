using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class PointsTransactionRepository(
    LcbDbContext context,
    ILogger<PointsTransactionRepository> logger)
    : RepositoryBase(logger), IPointsTransactionRepository
{
    public async Task<bool> CreateAsync(PointsTransactionEntity transaction)
        => await ExecuteAsync(async () =>
        {
            await context.PointsTransactions.AddAsync(transaction);
            return await context.SaveChangesAsync() > 0;
        }, nameof(CreateAsync));

    public async Task<IEnumerable<PointsTransactionEntity>> GetByContextAsync(ProviderTypeEnum provider, string channelId, string userId)
        => await ExecuteAsync(async () =>
        {
            return await context.PointsTransactions
                .AsNoTracking()
                .Where(x =>
                    x.Provider == provider &&
                    x.ChannelId == channelId &&
                    x.UserId == userId)
                .OrderBy(x => x.TransactionDateTime)
                .ToListAsync() as IEnumerable<PointsTransactionEntity>;
        }, nameof(GetByContextAsync));
}
