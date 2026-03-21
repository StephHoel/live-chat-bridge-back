using Microsoft.Extensions.Logging.Console;

namespace LCB.Api.DependencyInjection;

public static class LoggingDependencies
{
    public static ILoggingBuilder AddLogging(this ILoggingBuilder logging, IConfiguration configuration)
    {
        logging.ClearProviders();
        logging.AddConfiguration(configuration.GetSection("Logging"));
        logging.AddConsole(options =>
        {
            options.FormatterName = ConsoleFormatterNames.Simple;
        });
        logging.AddDebug();

        return logging;
    }
    public static IServiceCollection ConfigureLogging(this IServiceCollection services)
    {
        services.Configure<SimpleConsoleFormatterOptions>(options => options.IncludeScopes = true);

        return services;
    }
}