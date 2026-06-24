using System.Diagnostics.CodeAnalysis;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Message.Ingest;
using LCB.Application.Commands.Register;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Data;
using LCB.Infrastructure.Repositories;
using LCB.Infrastructure.Services;
using LCB.Infrastructure.Services.Adapter;
using LCB.Infrastructure.Services.Auth;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LCB.Application.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddScoped<LoginHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<MessageIngestHandler>();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddDbContext<LcbDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lcb.db";
            var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);

            if (!Path.IsPathRooted(sqliteConnection.DataSource))
            {
                var srcRootPath = Directory.GetParent(hostEnvironment.ContentRootPath)?.FullName
                                  ?? hostEnvironment.ContentRootPath;

                sqliteConnection.DataSource = Path.GetFullPath(
                    Path.Combine(srcRootPath, sqliteConnection.DataSource));
            }

            options.UseSqlite(sqliteConnection.ToString());
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, ChatMessageRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAdapterService, AdapterService>();

        return services;
    }
}