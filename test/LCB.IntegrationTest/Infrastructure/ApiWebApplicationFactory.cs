using LCB.Api;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Data;
using LCB.IntegrationTest.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LCB.IntegrationTest.Infrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtTestKey = "integration-tests-jwt-key-with-at-least-32-bytes!!";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ApiWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("JWT_KEY", JwtTestKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                ["JWT_KEY"] = JwtTestKey,
                ["Usernames:Tiktok"] = "",
                ["Usernames:Twitch"] = "",
                ["Usernames:Youtube"] = "",
                ["PasswordPolicy:MinLength"] = "8",
                ["PasswordPolicy:RequireUppercase"] = "true",
                ["PasswordPolicy:RequireLowercase"] = "true",
                ["PasswordPolicy:RequireDigit"] = "true",
                ["PasswordPolicy:RequireSpecialCharacter"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LcbDbContext>));
            if (dbDescriptor is not null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<LcbDbContext>(options => options.UseSqlite(_connection));

            // Keep migrations flow identical to production while preventing external/background effects in tests.
            services.RemoveAll<IHostedService>();
            services.RemoveAll<ITikTokChatProvider>();
            services.AddSingleton<ITikTokChatProvider, FakeTikTokChatProvider>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Environment.SetEnvironmentVariable("JWT_KEY", null);
            _connection.Dispose();
        }
    }
}
