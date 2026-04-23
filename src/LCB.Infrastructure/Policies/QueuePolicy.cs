using LCB.Domain.Entities;
using LCB.Domain.Enums;

namespace LCB.Infrastructure.Policies;

public static class QueuePolicy
{
    public static bool ShouldJoinQueue(this ChatMessage message)
    {
        var messageNormalized = message.Text.Trim().ToLowerInvariant();

        return message.Provider switch
        {
            ProviderTypeEnum.TIKTOK => TikTokQueuePolicy(messageNormalized),
            ProviderTypeEnum.TWITCH => TwitchQueuePolicy(messageNormalized),
            ProviderTypeEnum.YOUTUBE => YouTubeQueuePolicy(messageNormalized),
            _ => false,
        };
    }

    private static bool TikTokQueuePolicy(string message)
    {
        // TODO ajustar regra
        return message == "fila" || message == "!fila";
    }

    private static bool YouTubeQueuePolicy(string message)
    {
        // TODO ajustar regra
        return message == "/fila" || message == "!fila";
    }

    private static bool TwitchQueuePolicy(string message)
    {
        // TODO ajustar regra
        return message == "!fila";
    }
}