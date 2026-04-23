using LCB.Api.DependencyInjection;
using LCB.Api.Endpoints;
using LCB.Api.Extensions;
using LCB.Api.Json;
using LCB.Api.Middleware;
using LCB.Application.DependencyInjection;

namespace LCB.Api;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddLogging(builder.Configuration);

        builder.Services.ConfigureLogging();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddHandlers();
        builder.Services.AddRepositories();
        builder.Services.AddServices();
        builder.Services.AddSwagger();

        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        // Permissive DateTime converter for inbound JSON bodies (accepts ISO strings, epoch numbers, empty strings)
        builder.Services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new PermissiveDateTimeConverter());
        });

        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseDevelopSwagger();

        app.MapAuthEndpoints();
        app.MapMessageEndpoints();

        app.Run();
    }
}