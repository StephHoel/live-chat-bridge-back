using System.Threading.Channels;
using LCB.Domain.Models;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Services;

public class ChatProcessorService(ChannelReader<ChatMessageModel> Reader,
                                  ILogger<ChatProcessorService> Logger)
{
    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting {service}", nameof(ChatProcessorService));

        await foreach (var message in Reader.ReadAllAsync(cancellationToken))
        {
            var domainMessage = message.ToChatMessageEntity();

            // TODO lógica aqui
            // Ex: Salvar no banco, enviar para o frontend via SignalR, etc.
            Logger.LogInformation(
                "[{Platform}] [{CreatedAt}] {User}: {Text}",
                domainMessage.Provider, domainMessage.Timestamp, domainMessage.Author, domainMessage.Text);
        }

        Logger.LogInformation("Finishing {service}", nameof(ChatProcessorService));
    }
}