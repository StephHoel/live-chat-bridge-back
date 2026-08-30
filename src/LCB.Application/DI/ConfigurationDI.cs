using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Models.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LCB.Application.DI;

[ExcludeFromCodeCoverage]
public static class ConfigurationDI
{
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LiveConfig>(configuration.GetSection(LiveConfig.SectionName));
        services.Configure<AuditRetentionPolicy>(configuration.GetSection(AuditRetentionPolicy.SectionName));

        return services;
    }
}