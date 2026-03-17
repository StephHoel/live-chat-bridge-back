namespace LCB.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    public static User Create(string email, string passwordHash)
    {
        return new User
        {
            Email = email,
            PasswordHash = passwordHash
        };
    }
}