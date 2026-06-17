using System.Diagnostics.CodeAnalysis;

namespace LCB.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public static UserEntity Create(string email, string passwordHash)
    {
        return new UserEntity
        {
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}