using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Models.Config;
using LCB.Domain.Services;
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
using Microsoft.Extensions.Options;

namespace LCB.Infrastructure.DI;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    private static readonly string ConnectionStringDefault = "Data Source=lcb.db";

    public static IServiceCollection AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LcbDbContext>(configuration.AddContext);
        services.Configure<PasswordPolicy>(configuration.GetSection(nameof(PasswordPolicy)));
        services.AddScoped(AddPasswordValidator);

        services.AddRepositories();
        services.AddServices();

        return services;
    }

    public static void MigrateInfrastructure(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LcbDbContext>();
        dbContext.Database.Migrate();
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, ChatMessageRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<ILiveSettingsRepository, LiveSettingsRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IPointsBalanceRepository, PointsBalanceRepository>();
        services.AddScoped<IPointsTransactionRepository, PointsTransactionRepository>();
        services.AddScoped<IPointsIntegrationTypeCatalogRepository, PointsIntegrationTypeCatalogRepository>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IRecoverAntiAbuseService, RecoverAntiAbuseService>();
        services.AddScoped<IRecoverTokenService, RecoverTokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAdapterService, AdapterService>();
        services.AddScoped<IPointsService, PointsService>();

        return services;
    }

    private static void AddContext(this IConfiguration configuration, IServiceProvider sp, DbContextOptionsBuilder options)
    {
        var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
        var sqliteConnection = configuration.BuildSqliteConnection();

        if (!Path.IsPathRooted(sqliteConnection.DataSource))
        {
            var srcRootPath = Directory.GetParent(hostEnvironment.ContentRootPath)?.FullName;
            srcRootPath ??= hostEnvironment.ContentRootPath;

            sqliteConnection.DataSource = Path.GetFullPath(Path.Combine(srcRootPath, sqliteConnection.DataSource));
        }

        options.UseSqlite(sqliteConnection.ToString());
    }

    private static SqliteConnectionStringBuilder BuildSqliteConnection(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        connectionString ??= ConnectionStringDefault;
        return new SqliteConnectionStringBuilder(connectionString);
    }

    private static PasswordValidator AddPasswordValidator(IServiceProvider sp)
    {
        var passwordPolicy = sp.GetRequiredService<IOptions<PasswordPolicy>>();
        return new PasswordValidator(passwordPolicy.Value);
    }
}