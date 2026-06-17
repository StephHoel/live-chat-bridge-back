using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class InMemoryQueueRepository(ILogger<InMemoryQueueRepository> Logger)
    : InMemoryRepositoryBase<QueueEntity>(Logger), IQueueRepository
{
    #region Public Methods

    public Task<IEnumerable<QueueEntity>> GetAllAsync()
        => Read(GetAll, nameof(GetAllAsync));

    public Task<QueueEntity?> GetByUserAsync(string user)
        => Read(GetByUser(user), nameof(GetByUserAsync));

    public Task<bool> UpdateAsync(IEnumerable<QueueEntity> queues)
        => Write(Update(queues), nameof(UpdateAsync));

    public Task<bool> UserExistsAsync(string user)
        => Read(UserExists(user), nameof(UserExistsAsync));

    public Task<bool> DeleteAsync(QueueEntity queue)
        => Write(Delete(queue), nameof(DeleteAsync));

    public Task<bool> DeleteAllAsync()
        => Write(DeleteAll(), nameof(DeleteAllAsync));

    #endregion Public Methods

    #region Private Methods

    private static IEnumerable<QueueEntity> GetAll(IReadOnlyList<QueueEntity> list)
        => list.ToList().AsReadOnly().AsEnumerable();

    private static Func<IReadOnlyList<QueueEntity>, QueueEntity?> GetByUser(string user)
        => list => list.FirstOrDefault(x => x.User == user);

    private static Func<List<QueueEntity>, bool> Update(IEnumerable<QueueEntity> queues)
        => list =>
            {
                var l = queues.ToList();
                l.ForEach(q => list.RemoveAll(x => x.Id == q.Id));
                list.AddRange(l);
                return true;
            };

    private static Func<IReadOnlyList<QueueEntity>, bool> UserExists(string user)
        => list => list.Any(x => x.User == user);

    private static Func<List<QueueEntity>, bool> Delete(QueueEntity queue)
        => list => list.RemoveAll(x => x.Id == queue.Id) > 0;

    private static Func<List<QueueEntity>, bool> DeleteAll()
        => list => { list.Clear(); return true; };

    #endregion Private Methods
}
