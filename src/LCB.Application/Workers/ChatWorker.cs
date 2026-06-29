using LCB.Application.Helpers;
using LCB.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Workers;

public class ChatWorker(ChatProcessorService Service,
                        ILogger<ChatWorker> Logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken)
        => OperationExecutor.ExecuteAsync(Logger, nameof(ChatWorker), () => RunAsync(cancellationToken));

    private Task RunAsync(CancellationToken cancellationToken)
        => Service.ProcessMessagesAsync(cancellationToken);
}