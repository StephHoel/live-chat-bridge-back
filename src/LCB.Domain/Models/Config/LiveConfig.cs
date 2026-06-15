namespace LCB.Domain.Models.Config;

public record LiveConfig
{
    public const string SectionName = "Usernames";

    public string Tiktok { get; init; } = string.Empty;
    public string Twitch { get; init; } = string.Empty;
    public string Youtube { get; init; } = string.Empty;
}