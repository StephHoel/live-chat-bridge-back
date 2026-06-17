using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LCB.Infrastructure.Repositories.Base;

namespace LCB.Infrastructure.Repositories;

public class ChatMessageRepository(LcbDbContext context,
                                   ILogger<ChatMessageRepository> logger)
    : RepositoryBase(logger), IMessageRepository
{
    public async Task<bool> CreateAsync(IEnumerable<ChatMessageEntity> messages)
        => await ExecuteAsync(async () =>
        {
            await context.ChatMessages.AddRangeAsync(messages);
            return await context.SaveChangesAsync() > 0;
        }, nameof(CreateAsync));

    public async Task<ChatMessageEntity?> GetByIdempotencyKeyAsync(string idempotencyKey)
        => await ExecuteAsync(() =>
        {
            return context.ChatMessages.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
        }, nameof(GetByIdempotencyKeyAsync));

    public async Task<IEnumerable<ChatMessageEntity>> GetAllAsync()
        => await ExecuteAsync(() =>
        {
            return context.ChatMessages.AsNoTracking()
                                       .ToListAsync();
        }, nameof(GetAllAsync));

    public async Task<IEnumerable<ChatMessageEntity>> GetByProviderAsync(ProviderTypeEnum provider)
        => await ExecuteAsync(() =>
        {
            return context.ChatMessages.AsNoTracking()
                                       .Where(x => x.Provider == provider)
                                       .ToListAsync();
        }, nameof(GetByProviderAsync));

    public async Task<bool> UpdateAsync(IEnumerable<ChatMessageEntity> messages)
        => await ExecuteAsync(async () =>
        {
            var updates = messages.ToList();

            foreach (var message in updates)
            {
                var existing = await context.ChatMessages.FirstOrDefaultAsync(x => x.Id == message.Id);

                if (existing is null)
                {
                    message.TouchUpdatedAt();
                    await context.ChatMessages.AddAsync(message);
                }
                else
                {
                    existing.Provider = message.Provider;
                    existing.Author = message.Author;
                    existing.Text = message.Text;
                    existing.Timestamp = message.Timestamp;
                    existing.Processed = message.Processed;
                    existing.IdempotencyKey = message.IdempotencyKey;
                    existing.TouchUpdatedAt();
                }
            }

            return await context.SaveChangesAsync() > 0;
        }, nameof(UpdateAsync));

    public async Task<bool> DeleteAsync(ChatMessageEntity message)
        => await ExecuteAsync(async () =>
        {
            var existing = await context.ChatMessages.FirstOrDefaultAsync(x => x.Id == message.Id);

            if (existing is null)
                return false;

            context.ChatMessages.Remove(existing);

            return await context.SaveChangesAsync() > 0;
        }, nameof(DeleteAsync));

    public async Task<bool> DeleteAllAsync()
        => await ExecuteAsync(async () =>
        {
            var entities = await context.ChatMessages.ToListAsync();

            if (entities.Count == 0)
                return true;

            context.ChatMessages.RemoveRange(entities);

            return await context.SaveChangesAsync() > 0;
        }, nameof(DeleteAllAsync));
}
