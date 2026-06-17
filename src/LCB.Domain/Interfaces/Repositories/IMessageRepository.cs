using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<bool> CreateAsync(IEnumerable<ChatMessageEntity> messages);
    Task<ChatMessageEntity?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IEnumerable<ChatMessageEntity>> GetAllAsync();
    Task<IEnumerable<ChatMessageEntity>> GetByProviderAsync(ProviderTypeEnum provider);
    Task<bool> UpdateAsync(IEnumerable<ChatMessageEntity> messages);
    Task<bool> DeleteAsync(ChatMessageEntity message);
    Task<bool> DeleteAllAsync();
}