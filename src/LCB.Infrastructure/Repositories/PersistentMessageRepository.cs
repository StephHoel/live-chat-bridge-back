using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class PersistentMessageRepository : IMessageRepository
{
    private readonly LcbDbContext Context;
    private readonly ILogger<PersistentMessageRepository> Logger;

    public PersistentMessageRepository(LcbDbContext context, ILogger<PersistentMessageRepository> logger)
    {
        Context = context;
        Logger = logger;
    }

    public async Task<bool> CreateAsync(IEnumerable<ChatMessageEntity> messages)
    {
        await Context.ChatMessages.AddRangeAsync(messages);
        return await Context.SaveChangesAsync() > 0;
    }

    public async Task<ChatMessageEntity?> GetByIdempotencyKeyAsync(string idempotencyKey)
        => await Context.ChatMessages.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);

    public async Task<IEnumerable<ChatMessageEntity>> GetAllAsync()
        => await Context.ChatMessages.AsNoTracking().ToListAsync();

    public async Task<IEnumerable<ChatMessageEntity>> GetByProviderAsync(ProviderTypeEnum provider)
        => await Context.ChatMessages.AsNoTracking().Where(x => x.Provider == provider).ToListAsync();

    public async Task<bool> UpdateAsync(IEnumerable<ChatMessageEntity> messages)
    {
        var updates = messages.ToList();

        foreach (var message in updates)
        {
            var existing = await Context.ChatMessages.FirstOrDefaultAsync(x => x.Id == message.Id);

            if (existing is null)
                await Context.ChatMessages.AddAsync(message);
            else
                Context.Entry(existing).CurrentValues.SetValues(message);
        }

        return await Context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(ChatMessageEntity message)
    {
        var existing = await Context.ChatMessages.FirstOrDefaultAsync(x => x.Id == message.Id);

        if (existing is null)
            return false;

        Context.ChatMessages.Remove(existing);

        return await Context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAllAsync()
    {
        var entities = await Context.ChatMessages.ToListAsync();

        if (entities.Count == 0)
            return true;

        Context.ChatMessages.RemoveRange(entities);

        return await Context.SaveChangesAsync() > 0;
    }
}
