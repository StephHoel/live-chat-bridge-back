using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface ILiveSettingsRepository
{
    Task<LiveSettingsEntity?> GetByUserIdAsync(Guid userId);
    Task<bool> UpsertAsync(LiveSettingsEntity settings);
}
