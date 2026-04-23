using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface IQueueRepository
{
    Task<IEnumerable<Queue>> GetAllAsync();
    Task<Queue?> GetByUserAsync(string user);
    Task<bool> UpdateAsync(IEnumerable<Queue> queues);
    Task<bool> UserExistsAsync(string user);
    Task<bool> DeleteAsync(Queue queue);
    Task<bool> DeleteAllAsync();
}