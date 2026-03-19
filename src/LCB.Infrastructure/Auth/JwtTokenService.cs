using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LCB.Application.Interfaces;
using LCB.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace LCB.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    public string GenerateToken(User user)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_KEY") ?? "super-secret-key-123-super-secret-key-123";
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        if (keyBytes.Length < 32)
            throw new ArgumentException("JWT key must be at least 256 bits (32 bytes). Set environment variable 'JWT_KEY' to a secure 32+ byte value.");

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

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}