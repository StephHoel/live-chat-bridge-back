using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface IQueueRepository
{
    Task<IEnumerable<QueueEntity>> GetAllAsync();
    Task<QueueEntity?> GetByUserAsync(string user);
    Task<bool> UpdateAsync(IEnumerable<QueueEntity> queues);
    Task<bool> UserExistsAsync(string user);
    Task<bool> DeleteAsync(QueueEntity queue);
    Task<bool> DeleteAllAsync();
}