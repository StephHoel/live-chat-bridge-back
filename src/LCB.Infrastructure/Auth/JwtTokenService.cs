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
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super-secret-key-123"));
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