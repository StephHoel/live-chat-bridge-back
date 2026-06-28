using System.Threading.Channels;
using LCB.Domain.Extensions;
using LCB.Domain.Models;
using Microsoft.Extensions.Logging;
using TikTokLiveSharp.Client;
using TikTokLiveSharp.Events;
using TikTokLiveSharp.Events.Objects;

namespace LCB.Infrastructure.Providers;

public class TikTokChatProvider(ChannelWriter<ChatMessageModel> Writer,
                                ILogger<TikTokChatProvider> Logger)
{
    private TikTokLiveClient? Client;

    public void Connect(string tiktokUsername, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting {provider}", nameof(TikTokChatProvider));

        Client = new TikTokLiveClient(hostId: tiktokUsername, settings: new()
        {
            SkipRoomInfo = true, // IMPORTANTE: Pular o scraping que está dando erro
            HandleExistingMessagesOnConnect = true,
            DownloadGiftInfo = true,
            ClientLanguage = "pt-BR",
            RetryOnConnectionFailure = true,
            PrintToConsole = true // TODO mudar para false para não sujar o log
        });

        Client.OnChatMessage += OnChatMessage;
        Client.OnGift += OnGift;
        // Client.OnLike += OnLike;
        Client.OnException += OnException;

        try
        {
            Logger.LogInformation("Iniciando escuta da live de @{username}", tiktokUsername);

            Client.Run(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro fatal no TikTokLiveClient");
        }

        Logger.LogInformation("Finishing {provider}", nameof(TikTokChatProvider));
    }

    private void OnChatMessage(TikTokLiveClient _, Chat args)
    {
        var message = new ChatMessageModel(
            args.Sender.UniqueId,
            args.Message,
            "TikTok",
            args.TimeStamp.ToDateTime()
        );

        Writer.TryWrite(message);
    }

    private void OnGift(TikTokLiveClient _, TikTokGift args)
    {
        var giftName = args.Gift?.Name ?? "Presente Desconhecido";
        var userName = args.Sender?.NickName ?? "Usuário";
        var amount = args.Amount; // Quantidade (ex: 1 rosa, 5 rosas)

        // TODO ajustar aqui
        Logger.LogInformation("[TIKTOK] PRESENTE: {UserName} enviou {Amount}x {GiftName}", userName, amount, giftName);
    }

    private void OnLike(TikTokLiveClient _, Like args)
    {
        var count = args.Count;
        var total = args.Total;
        var userName = args.Sender?.UniqueId ?? "Alguém";

        // TODO ajustar aqui
        Logger.LogInformation("[TIKTOK] LIKE: {UserName} curtiu {Count} vezes (Total da Live: {Total})", userName, count, total);
    }

    private void OnException(object? _, Exception args)
    {
        Logger.LogError(args, $"Erro de Conexão | Mensagem: {args.Message}");
    }
}