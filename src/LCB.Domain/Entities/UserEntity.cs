using System.Diagnostics.CodeAnalysis;

namespace LCB.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    public static UserEntity Create(string email, string passwordHash)
    {
        return new UserEntity
        {
            Email = email,
            PasswordHash = passwordHash
        };
    }
}