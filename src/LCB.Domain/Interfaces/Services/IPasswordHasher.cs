namespace LCB.Domain.Interfaces.Services;

/// <summary>
/// Contract for password hashing and verification services.
/// Implementations should use secure algorithms like PBKDF2 or bcrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using a secure algorithm.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A salted hash of the password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool Verify(string password, string hash);
}
