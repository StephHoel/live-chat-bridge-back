using LCB.Application.Helpers;
using LCB.Application.Services;
using LCB.Domain.Models.Config;
using LCB.Infrastructure.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LCB.Application.Workers;

public class ChatWorker(TikTokChatProvider TikTok,
                        ChatProcessorService Service,
                        ILogger<ChatWorker> Logger,
                        IOptionsMonitor<LiveConfig> Options)
    : BackgroundService
{
    private readonly LiveConfig Config = Options.CurrentValue;

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
        => OperationExecutor.ExecuteAsync(Logger, nameof(ChatWorker), () => RunAsync(cancellationToken));

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var processorTask = Service.ProcessMessagesAsync(cancellationToken);

        var tiktokUsername = Config.Tiktok.Replace("@", "").Trim();
        if (string.IsNullOrEmpty(tiktokUsername))
        {
            Logger.LogWarning("Usuario do TikTok nao informado");
            return;
        }

        var tiktokTask = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TikTok.Connect(tiktokUsername, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Erro na conexão TikTok");

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }, cancellationToken);

        await Task.WhenAll(processorTask, tiktokTask);
    }
}