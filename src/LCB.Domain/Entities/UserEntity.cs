using System.Diagnostics.CodeAnalysis;
using LCB.Domain.Extensions;

namespace LCB.Domain.Entities;

[ExcludeFromCodeCoverage]
public class UserEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow.NormalizeToUtcMinus3();

    public static UserEntity Create(string email, string passwordHash)
    {
        return new UserEntity
        {
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow.NormalizeToUtcMinus3(),
            UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3()
        };
    }

    public void TouchUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow.NormalizeToUtcMinus3();
    }
}