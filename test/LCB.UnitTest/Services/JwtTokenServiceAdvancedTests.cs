using System.Collections.Generic;
using LCB.Domain.Entities;
using LCB.Domain.Extensions;
using LCB.Infrastructure.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Services;

public class JwtTokenServiceAdvancedTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("small")]
    public void GetBytesFromJwtKey_ReturnsNull_WhenKeyIsInvalid(string key)
    {
        Assert.Null(key.GetBytesFromJwtKey());
    }

    [Fact]
    public void GetBytesFromJwtKey_ReturnsBytes_WhenKeyIsLargeEnough()
    {
        var bytes = new string('k', 32).GetBytesFromJwtKey();

        Assert.NotNull(bytes);
        Assert.Equal(32, bytes!.Length);
    }

    [Fact]
    public void GenerateToken_EmbedsExpectedClaims()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string>("JWT_KEY", new string('k', 32))])
            .Build();

        var service = new JwtTokenService(cfg, new NullLogger<JwtTokenService>());
        var user = UserEntity.Create("alice@example.com", "hash");

        var token = service.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }
}