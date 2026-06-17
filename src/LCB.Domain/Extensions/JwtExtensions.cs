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
            var secret = key ?? "sup3r-s3gr3d0-qu3-n40-d3v3-s3r-r3v3l4d0";

            var keyBytes = Encoding.UTF8.GetBytes(secret);

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