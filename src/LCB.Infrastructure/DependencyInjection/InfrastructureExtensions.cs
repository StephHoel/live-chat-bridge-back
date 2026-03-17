using LCB.Application.Interfaces;
using LCB.Domain.Repositories;
using LCB.Infrastructure.Auth;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, InMemoryUserRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}