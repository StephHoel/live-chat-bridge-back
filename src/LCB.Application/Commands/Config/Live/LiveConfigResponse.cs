namespace LCB.Application.Commands.Config.Live;

public class LiveConfigResponse
{
    public string? TikTokUsername { get; set; }
    public string? TwitchUsername { get; set; }
    public string? YouTubeUsername { get; set; }
    public long ReloadTimeInSec { get; set; }
}
