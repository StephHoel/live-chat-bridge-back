using LCB.Application.Commands.Login;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Repositories;
using LCB.Infrastructure.Services.Adapter;
using LCB.Infrastructure.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<MessageIngestHandler>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<IMessageRepository, InMemoryMessageRepository>();
        services.AddScoped<IQueueRepository, InMemoryQueueRepository>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAdapterService, AdapterService>();

        return services;
    }
}