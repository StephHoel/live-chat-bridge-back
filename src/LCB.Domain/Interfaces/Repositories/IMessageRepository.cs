using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Domain.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<bool> CreateAsync(IEnumerable<ChatMessage> messages);
    Task<ChatMessage?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IEnumerable<ChatMessage>> GetAllAsync();
    Task<IEnumerable<ChatMessage>> GetByProviderAsync(ProviderTypeEnum provider);
    Task<bool> UpdateAsync(IEnumerable<ChatMessage> messages);
    Task<bool> DeleteAsync(ChatMessage message);
    Task<bool> DeleteAllAsync();
}