using System.Collections.Generic;
using LCB.Infrastructure.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsToken_WhenKeyIsValid()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string>("JWT_KEY", new string('k', 32))
            ])
            .Build();

        var service = new JwtTokenService(cfg, new NullLogger<JwtTokenService>());
        var user = Domain.Entities.User.Create("alice@example.com", "hash");

        var token = service.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_ReturnsEmpty_WhenKeyIsInvalid()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string>("JWT_KEY", "small")
            ])
            .Build();

        var service = new JwtTokenService(cfg, new NullLogger<JwtTokenService>());
        var user = Domain.Entities.User.Create("bob@example.com", "hash");

        var token = service.GenerateToken(user);

        Assert.Equal(string.Empty, token);
    }
}
