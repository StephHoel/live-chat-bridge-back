using System.Diagnostics.CodeAnalysis;
using LCB.Application.Commands.Config.Live.Get;
using LCB.Application.Commands.Config.Live.Put;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Message.Ingest;
using LCB.Application.Commands.Queue.Get;
using LCB.Application.Commands.Recover;
using LCB.Application.Commands.Register;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
using LCB.Application.Commands.Worker.Stop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InfraDI = LCB.Infrastructure.DI.DependencyInjection;

namespace LCB.Application.DI;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration) => InfraDI.AddInfrastructure(services, configuration);

    public static void MigrateInfrastructure(this IServiceProvider serviceProvider)
        => InfraDI.MigrateInfrastructure(serviceProvider);

    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<RecoverHandler>();
        services.AddScoped<MessageIngestHandler>();
        services.AddScoped<GetQueueHandler>();
        services.AddScoped<GetLiveConfigHandler>();
        services.AddScoped<PutLiveConfigHandler>();
        services.AddScoped<StartWorkerHandler>();
        services.AddScoped<StopWorkerHandler>();
        services.AddScoped<GetWorkerStatusHandler>();

        return services;
    }
}