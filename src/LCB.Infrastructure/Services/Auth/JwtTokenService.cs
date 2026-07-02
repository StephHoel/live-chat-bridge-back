using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LCB.Domain.Entities;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LCB.Infrastructure.Services.Auth;

public class JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger) : ITokenService
{
    private readonly byte[]? _keyBytes = configuration["JWT_KEY"].GetBytesFromJwtKey();

    public string GenerateToken(UserEntity user)
    {
        try
        {
            logger.LogInformation("[{method}] Starting JWT generation for user {UserId}", [nameof(GenerateToken), user.Id]);

            var key = new SymmetricSecurityKey(_keyBytes ?? []);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var written = new JwtSecurityTokenHandler().WriteToken(token);

            logger.LogDebug("JWT generated for user {UserId}", user.Id);

            return written;
        }
        catch (Exception e)
        {
            logger.LogError("Error unexpected | Message: {Message} | StackTrace: {Stack}", [e.Message, e.StackTrace]);
            return string.Empty;
        }
        finally
        {
            logger.LogInformation("[{method}] Finishing JWT generation", nameof(GenerateToken));
        }
    }
}