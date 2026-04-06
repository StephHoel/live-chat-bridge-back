using LCB.Application.Commands.Login;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Repositories;
using LCB.Infrastructure.Services.Auth;
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