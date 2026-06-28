using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class LiveSettingsRepository(LcbDbContext context,
                                    ILogger<LiveSettingsRepository> logger)
    : RepositoryBase(logger), ILiveSettingsRepository
{
    public async Task<LiveSettingsEntity?> GetByUserIdAsync(Guid userId)
        => await ExecuteAsync(() => context.LiveSettings.AsNoTracking()
                                           .FirstOrDefaultAsync(x => x.UserId == userId),
                                    nameof(GetByUserIdAsync));

    public async Task<bool> UpsertAsync(LiveSettingsEntity settings)
        => await ExecuteAsync(async () =>
        {
            var existing = await context.LiveSettings.FirstOrDefaultAsync(x => x.UserId == settings.UserId);

            if (existing is null)
            {
                await context.LiveSettings.AddAsync(settings);
            }
            else
            {
                existing.Update(settings.TikTokUsername,
                                settings.TwitchUsername,
                                settings.YouTubeUsername,
                                settings.ReloadTimeInSec,
                                settings.UpdatedByUser);
            }

            return await context.SaveChangesAsync() >= 0;
        }, nameof(UpsertAsync));
}
