using LCB.Application.Helpers;
using LCB.Domain.Entities;

namespace LCB.Application.Commands.Config.Live;

public static class LiveConfigMapper
{
    public static LiveConfigResponse ToResponse(this LiveSettingsEntity settings)
    {
        return new LiveConfigResponse
        {
            TikTokUsername = LiveUsernameNormalizer.Normalize(settings.TikTokUsername),
            TwitchUsername = LiveUsernameNormalizer.Normalize(settings.TwitchUsername),
            YouTubeUsername = LiveUsernameNormalizer.Normalize(settings.YouTubeUsername),
            ReloadTimeInSec = settings.ReloadTimeInSec
        };
    }
}