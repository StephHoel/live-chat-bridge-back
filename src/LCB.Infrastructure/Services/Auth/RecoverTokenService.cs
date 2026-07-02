using System.Security.Cryptography;
using LCB.Domain.Interfaces.Services;

namespace LCB.Infrastructure.Services.Auth;

public class RecoverTokenService : IRecoverTokenService
{
    public string GenerateTemporaryResetToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
