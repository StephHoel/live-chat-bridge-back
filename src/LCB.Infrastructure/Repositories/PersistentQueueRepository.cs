using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LCB.Infrastructure.Repositories;

public class PersistentQueueRepository(LcbDbContext context) : IQueueRepository
{
    public async Task<IEnumerable<QueueEntity>> GetAllAsync()
        => await context.Queues.AsNoTracking().ToListAsync();

    public async Task<QueueEntity?> GetByUserAsync(string user)
        => await context.Queues.AsNoTracking().FirstOrDefaultAsync(x => x.User == user);

    public async Task<bool> UpdateAsync(IEnumerable<QueueEntity> queues)
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
    }

    public async Task<bool> UserExistsAsync(string user)
        => await context.Queues.AnyAsync(x => x.User == user);

    public async Task<bool> DeleteAsync(QueueEntity queue)
    {
        var existing = await context.Queues.FirstOrDefaultAsync(x => x.Id == queue.Id);

        if (existing is null)
            return false;

        context.Queues.Remove(existing);

        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAllAsync()
    {
        var entities = await context.Queues.ToListAsync();

        if (entities.Count == 0)
            return true;

        context.Queues.RemoveRange(entities);

        return await context.SaveChangesAsync() > 0;
    }
}
