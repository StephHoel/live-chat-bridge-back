using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LCB.Domain.Entities;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LCB.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    private readonly string _secret;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _logger = logger;
        var keyBytes = configuration["JWT_KEY"].GetBytesFromJwtKey();
        _secret = configuration["JWT_KEY"] ?? string.Empty;

        if (keyBytes == null)
            throw new ArgumentException("JWT key bytes could not be obtained from configuration.");

        _logger.LogInformationWithMethod("JwtTokenService initialized with key length {KeyLength} bytes.", new object?[] { keyBytes.Length });
    }

    public string GenerateToken(User user)
    {
        _logger.LogInformationWithMethod("Generating JWT for user {UserId} ({Email}).", new object?[] { user.Id, user.Email });

        var keyBytes = Encoding.UTF8.GetBytes(_secret);
        var key = new SymmetricSecurityKey(keyBytes);
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
        _logger.LogDebugWithMethod("JWT generated for user {UserId}.", new object?[] { user.Id });
        return written;
    }
}