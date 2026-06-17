using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class InMemoryMessageRepository(ILogger<InMemoryMessageRepository> Logger)
    : InMemoryRepositoryBase<ChatMessageEntity>(Logger), IMessageRepository
{
    #region Public Methods

    public Task<bool> CreateAsync(IEnumerable<ChatMessageEntity> messages)
        => Write(Create(messages), nameof(CreateAsync));

    public Task<ChatMessageEntity?> GetByIdempotencyKeyAsync(string idempotencyKey)
        => Read(GetByIdempotencyKey(idempotencyKey), nameof(GetByIdempotencyKeyAsync));

    public Task<IEnumerable<ChatMessageEntity>> GetAllAsync()
        => Read(GetAll(), nameof(GetAllAsync));

    public Task<IEnumerable<ChatMessageEntity>> GetByProviderAsync(ProviderTypeEnum provider)
        => Read(GetByProvider(provider), nameof(GetByProviderAsync));

    public Task<bool> UpdateAsync(IEnumerable<ChatMessageEntity> messages)
        => Write(Update(messages), nameof(UpdateAsync));

    public Task<bool> DeleteAsync(ChatMessageEntity message)
        => Write(Delete(message), nameof(DeleteAsync));

    public Task<bool> DeleteAllAsync()
        => Write(DeleteAll(), nameof(DeleteAllAsync));

    #endregion Public Methods

    #region Private Methods

    private static Func<List<ChatMessageEntity>, bool> Create(IEnumerable<ChatMessageEntity> messages)
        => list => { list.AddRange([.. messages]); return true; };

    private static Func<IReadOnlyList<ChatMessageEntity>, ChatMessageEntity?> GetByIdempotencyKey(string idempotencyKey)
        => list => list.FirstOrDefault(m => m.IdempotencyKey == idempotencyKey);

    private static Func<IReadOnlyList<ChatMessageEntity>, IEnumerable<ChatMessageEntity>> GetAll()
        => list => list.ToList().AsReadOnly().AsEnumerable();

    private static Func<IReadOnlyList<ChatMessageEntity>, IEnumerable<ChatMessageEntity>> GetByProvider(ProviderTypeEnum provider)
        => list => list.Where(m => m.Provider == provider).ToList().AsReadOnly().AsEnumerable();

    private static Func<List<ChatMessageEntity>, bool> Update(IEnumerable<ChatMessageEntity> messages)
        => list =>
            {
                var l = messages.ToList();
                l.ForEach(m => list.RemoveAll(x => x.Id == m.Id));
                list.AddRange(l);
                return true;
            };

    private static Func<List<ChatMessageEntity>, bool> Delete(ChatMessageEntity message)
        => list => list.RemoveAll(x => x.Id == message.Id) > 0;

    private static Func<List<ChatMessageEntity>, bool> DeleteAll()
        => list => { list.Clear(); return true; };

    #endregion Private Methods
}