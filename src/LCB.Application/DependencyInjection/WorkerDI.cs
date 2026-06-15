using System.Threading.Channels;
using LCB.Application.Services;
using LCB.Application.Workers;
using LCB.Domain.Models;
using LCB.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Application.DependencyInjection;

public static class WorkerDI
{
    public static IServiceCollection AddWorkers(this IServiceCollection services)
    {
        var chatChannel = Channel.CreateBounded<ChatMessage>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        services.AddSingleton(chatChannel);
        services.AddSingleton(chatChannel.Writer);
        services.AddSingleton(chatChannel.Reader);

        services.AddSingleton<TikTokChatProvider>();
        services.AddSingleton<ChatProcessorService>();

        services.AddHostedService<ChatWorker>();

        return services;
    }
}