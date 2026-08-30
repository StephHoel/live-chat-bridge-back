using LCB.Api.DependencyInjection;
using LCB.Api.Json;
using LCB.Api.Middleware;
using LCB.Application.DI;

namespace LCB.Api;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        builder.Logging.AddLogging(configuration);

        builder.Services.ConfigureLogging();
        builder.Services.AddJwtAuthentication(configuration);
        builder.Services.AddConfiguration(configuration);
        builder.Services.AddHandlers();
        builder.Services.AddInfrastructure(configuration);
        builder.Services.AddSwagger();
        builder.Services.AddWorkers();
        builder.Services.ConfigureAuthorization();
        builder.Services.AddControllers();

        // Permissive DateTime converter for inbound JSON bodies (accepts ISO strings, epoch numbers, empty strings)
        builder.Services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new PermissiveDateTimeConverter());
        });

        var app = builder.Build();

        app.Services.MigrateInfrastructure();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseDevelopSwagger();

        app.AddEndpoints();

        app.Run();
    }
}