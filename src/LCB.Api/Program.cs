using LCB.Api.DependencyInjection;
using LCB.Api.Endpoints;
using LCB.Api.Extensions;
using LCB.Api.Json;
using LCB.Api.Middleware;
using LCB.Application.DependencyInjection;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LCB.Api;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddLogging(builder.Configuration);

        builder.Services.ConfigureLogging();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddConfiguration(builder.Configuration);
        builder.Services.AddHandlers();
        builder.Services.AddRepositories();
        builder.Services.AddServices(builder.Configuration);
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

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LcbDbContext>();
            dbContext.Database.Migrate();
        }

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseDevelopSwagger();

        app.MapAuthEndpoints();
        app.MapMessageEndpoints();
        app.MapConfigEndpoints();

        app.Run();
    }
}