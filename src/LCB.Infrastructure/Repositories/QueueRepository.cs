using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LCB.Infrastructure.Repositories.Base;

namespace LCB.Infrastructure.Repositories;

public class QueueRepository(LcbDbContext context,
                             ILogger<QueueRepository> logger)
    : RepositoryBase(logger), IQueueRepository
{
    public async Task<IEnumerable<QueueEntity>> GetAllAsync()
        => await ExecuteAsync(() => context.Queues.AsNoTracking().ToListAsync(), nameof(GetAllAsync));

    public async Task<QueueEntity?> GetByUserAsync(string user)
        => await ExecuteAsync(() => context.Queues.AsNoTracking().FirstOrDefaultAsync(x => x.User == user), nameof(GetByUserAsync));

    public async Task<bool> UpdateAsync(IEnumerable<QueueEntity> queues)
        => await ExecuteAsync(async () =>
        {
            var updates = queues.ToList();

            foreach (var queue in updates)
            {
                var existing = await context.Queues.FirstOrDefaultAsync(x => x.Id == queue.Id);

                if (existing is null)
                    await context.Queues.AddAsync(queue);
                else
                    context.Entry(existing).CurrentValues.SetValues(queue);
            }

            return await context.SaveChangesAsync() > 0;
        }, nameof(UpdateAsync));

    public async Task<bool> UserExistsAsync(string user)
        => await ExecuteAsync(() => context.Queues.AnyAsync(x => x.User == user), nameof(UserExistsAsync));

    public async Task<bool> DeleteAsync(QueueEntity queue)
        => await ExecuteAsync(async () =>
        {
            var existing = await context.Queues.FirstOrDefaultAsync(x => x.Id == queue.Id);

            if (existing is null)
                return false;

            context.Queues.Remove(existing);

            return await context.SaveChangesAsync() > 0;
        }, nameof(DeleteAsync));

    public async Task<bool> DeleteAllAsync()
        => await ExecuteAsync(async () =>
        {
            var entities = await context.Queues.ToListAsync();

            if (entities.Count == 0)
                return true;

            context.Queues.RemoveRange(entities);

            return await context.SaveChangesAsync() > 0;
        }, nameof(DeleteAllAsync));
}
