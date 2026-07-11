using LCB.Domain.Enums;

namespace LCB.Infrastructure.Policies;

public static class PointsPolicy
{
    /// <summary>
    /// Returns the delta (points to add) for a given provider and integration type.
    /// Returns 0 when the combination is not supported.
    /// </summary>
    public static long GetDelta(ProviderTypeEnum provider, IntegrationTypeEnum integrationType)
    {
        return provider switch
        {
            ProviderTypeEnum.TIKTOK => TikTokDelta(integrationType),
            ProviderTypeEnum.TWITCH => TwitchDelta(integrationType),
            ProviderTypeEnum.YOUTUBE => YouTubeDelta(integrationType),
            _ => 0
        };
    }

    public static bool IsProviderSupported(ProviderTypeEnum provider)
        => provider is ProviderTypeEnum.TIKTOK or ProviderTypeEnum.TWITCH or ProviderTypeEnum.YOUTUBE;

    public static bool IsIntegrationTypeSupported(IntegrationTypeEnum integrationType)
        => integrationType is IntegrationTypeEnum.Message or IntegrationTypeEnum.Like;

    private static long TikTokDelta(IntegrationTypeEnum integrationType)
        => integrationType switch
        {
            IntegrationTypeEnum.Message => 10,
            IntegrationTypeEnum.Like => 1,
            _ => 0
        };

    private static long TwitchDelta(IntegrationTypeEnum integrationType)
        => integrationType switch
        {
            IntegrationTypeEnum.Message => 10,
            IntegrationTypeEnum.Like => 1,
            _ => 0
        };

    private static long YouTubeDelta(IntegrationTypeEnum integrationType)
        => integrationType switch
        {
            IntegrationTypeEnum.Message => 10,
            IntegrationTypeEnum.Like => 1,
            _ => 0
        };
}
