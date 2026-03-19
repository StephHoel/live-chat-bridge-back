using LCB.Application.Commands.Login;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Infrastructure.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();

        return services;
    }
}