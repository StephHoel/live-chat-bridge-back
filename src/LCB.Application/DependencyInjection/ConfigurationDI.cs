using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Application.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LiveConfig>(configuration.GetSection(LiveConfig.SectionName));

        return services;
    }
}