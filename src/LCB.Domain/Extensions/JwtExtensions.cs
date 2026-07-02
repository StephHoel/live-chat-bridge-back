using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LCB.Domain.Extensions;

[ExcludeFromCodeCoverage]
public static class JwtExtensions
{
    public static byte[]? GetBytesFromJwtKey(this string? key)
    {
        if (key is null)
            return null;

        try
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);

            if (keyBytes.Length < 32)
                throw new ArgumentException("JWT key must be at least 256 bits (32 bytes). Set 'JWT_KEY' in configuration to a secure 32+ byte value.");

            return keyBytes;
        }
        catch (Exception)
        {
            return null;
        }
    }
}