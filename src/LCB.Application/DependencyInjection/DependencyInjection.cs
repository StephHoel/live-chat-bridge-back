using LCB.Application.Commands.Login;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Repositories;
using LCB.Infrastructure.Auth;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Application.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, InMemoryUserRepository>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}