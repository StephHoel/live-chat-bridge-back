namespace LCB.Application.Commands.Config.Live.Put;

public record PutLiveConfigRequest(
    string? TikTokUsername,
    string? TwitchUsername,
    string? YouTubeUsername,
    long? ReloadTimeInSec);
