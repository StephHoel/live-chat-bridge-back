using System.Security.Cryptography;
using LCB.Domain.Interfaces.Services;

namespace LCB.Infrastructure.Services;

/// <summary>
/// Password hashing service using PBKDF2 with SHA256.
/// Uses a random salt and 10,000 iterations for each password.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int HashSize = 32; // 256 bits
    private const int SaltSize = 16; // 128 bits
    private const int Iterations = 10000;

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        // Format: base64(iterations_as_4_bytes) + base64(salt) + base64(hash)
        var iterationsBytes = BitConverter.GetBytes(Iterations);
        var combined = new byte[iterationsBytes.Length + salt.Length + hash.Length];

        Buffer.BlockCopy(iterationsBytes, 0, combined, 0, iterationsBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, iterationsBytes.Length, salt.Length);
        Buffer.BlockCopy(hash, 0, combined, iterationsBytes.Length + salt.Length, hash.Length);

        return Convert.ToBase64String(combined);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        if (string.IsNullOrEmpty(hash))
            return false;

        try
        {
            var combined = Convert.FromBase64String(hash);

            if (combined.Length < sizeof(int) + SaltSize)
                return false;

            var iterationsBytes = new byte[sizeof(int)];
            Buffer.BlockCopy(combined, 0, iterationsBytes, 0, sizeof(int));
            var iterations = BitConverter.ToInt32(iterationsBytes, 0);

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(combined, sizeof(int), salt, 0, SaltSize);

            var storedHash = new byte[combined.Length - sizeof(int) - SaltSize];
            Buffer.BlockCopy(combined, sizeof(int) + SaltSize, storedHash, 0, storedHash.Length);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                var computedHash = pbkdf2.GetBytes(HashSize);
                return ConstantTimeComparison(computedHash, storedHash);
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Performs a constant-time comparison of two byte arrays to prevent timing attacks.
    /// </summary>
    private static bool ConstantTimeComparison(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
