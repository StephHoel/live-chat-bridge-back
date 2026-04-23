using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class InMemoryQueueRepository(ILogger<InMemoryQueueRepository> Logger)
    : InMemoryRepositoryBase<Queue>(Logger), IQueueRepository
{
    #region Public Methods

    public Task<IEnumerable<Queue>> GetAllAsync()
        => Read(GetAll, nameof(GetAllAsync));

    public Task<Queue?> GetByUserAsync(string user)
        => Read(GetByUser(user), nameof(GetByUserAsync));

    public Task<bool> UpdateAsync(IEnumerable<Queue> queues)
        => Write(Update(queues), nameof(UpdateAsync));

    public Task<bool> UserExistsAsync(string user)
        => Read(UserExists(user), nameof(UserExistsAsync));

    public Task<bool> DeleteAsync(Queue queue)
        => Write(Delete(queue), nameof(DeleteAsync));

    public Task<bool> DeleteAllAsync()
        => Write(DeleteAll(), nameof(DeleteAllAsync));

    #endregion Public Methods

    #region Private Methods

    private static IEnumerable<Queue> GetAll(IReadOnlyList<Queue> list)
        => list.ToList().AsReadOnly().AsEnumerable();

    private static Func<IReadOnlyList<Queue>, Queue?> GetByUser(string user)
        => list => list.FirstOrDefault(x => x.User == user);

    private static Func<List<Queue>, bool> Update(IEnumerable<Queue> queues)
        => list =>
            {
                var l = queues.ToList();
                l.ForEach(q => list.RemoveAll(x => x.Id == q.Id));
                list.AddRange(l);
                return true;
            };

    private static Func<IReadOnlyList<Queue>, bool> UserExists(string user)
        => list => list.Any(x => x.User == user);

    private static Func<List<Queue>, bool> Delete(Queue queue)
        => list => list.RemoveAll(x => x.Id == queue.Id) > 0;

    private static Func<List<Queue>, bool> DeleteAll()
        => list => { list.Clear(); return true; };

    #endregion Private Methods
}
