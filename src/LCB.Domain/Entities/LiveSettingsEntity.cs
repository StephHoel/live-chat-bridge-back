using LCB.Domain.Extensions;

namespace LCB.Domain.Entities;

public class LiveSettingsEntity
{
    public Guid SettingsId { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string? TikTokUsername { get; private set; }
    public string? YouTubeUsername { get; private set; }
    public string? TwitchUsername { get; private set; }
    public long ReloadTimeInSec { get; private set; } = 5;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public string UpdatedByUser { get; private set; } = string.Empty;

    public static LiveSettingsEntity Create(
        Guid userId,
        string updatedByUser,
        string? tikTokUsername = null,
        string? twitchUsername = null,
        string? youTubeUsername = null,
        long reloadTimeInSec = 5)
    {
        return new LiveSettingsEntity
        {
            UserId = userId,
            TikTokUsername = tikTokUsername,
            TwitchUsername = twitchUsername,
            YouTubeUsername = youTubeUsername,
            ReloadTimeInSec = reloadTimeInSec,
            CreatedAtUtc = DateTime.UtcNow.NormalizeToUtcMinus3(),
            UpdatedAtUtc = DateTime.UtcNow.NormalizeToUtcMinus3(),
            UpdatedByUser = updatedByUser
        };
    }

    public void Update(
        string? tikTokUsername,
        string? twitchUsername,
        string? youTubeUsername,
        long reloadTimeInSec,
        string updatedByUser)
    {
        TikTokUsername = tikTokUsername;
        TwitchUsername = twitchUsername;
        YouTubeUsername = youTubeUsername;
        ReloadTimeInSec = reloadTimeInSec;
        UpdatedByUser = updatedByUser;
        UpdatedAtUtc = DateTime.UtcNow.NormalizeToUtcMinus3();
    }

    public void SetTikTokUsername(string? username)
        => TikTokUsername = username;

    public void SetTwitchUsername(string? username)
        => TwitchUsername = username;

    public void SetYouTubeUsername(string? username)
        => YouTubeUsername = username;

    public void SetReloadTimeInSec(long reloadTimeInSec)
        => ReloadTimeInSec = reloadTimeInSec;

    public void TouchUpdatedBy(string updatedByUser)
    {
        UpdatedByUser = updatedByUser;
        UpdatedAtUtc = DateTime.UtcNow.NormalizeToUtcMinus3();
    }
}
