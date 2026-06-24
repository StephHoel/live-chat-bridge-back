using LCB.Domain.Models.Config;

namespace LCB.Domain.Services;

/// <summary>
/// Validador de e-mail e senha conforme política configurável.
/// </summary>
public class PasswordValidator
{
    private readonly PasswordPolicy policy;

    public PasswordValidator(PasswordPolicy policy)
    {
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < policy.MinLength)
            return false;

        var hasUppercase = !policy.RequireUppercase || password.Any(char.IsUpper);
        var hasLowercase = !policy.RequireLowercase || password.Any(char.IsLower);
        var hasNumber = !policy.RequireDigit || password.Any(char.IsDigit);
        var hasSpecial = !policy.RequireSpecialCharacter || password.Any(c => !char.IsLetterOrDigit(c));

        return hasUppercase && hasLowercase && hasNumber && hasSpecial;
    }

    public string GetPasswordErrorMessage()
    {
        var requirements = new List<string>();

        if (policy.MinLength > 0)
            requirements.Add($"at least {policy.MinLength} characters");

        if (policy.RequireUppercase)
            requirements.Add("uppercase letter");

        if (policy.RequireLowercase)
            requirements.Add("lowercase letter");

        if (policy.RequireDigit)
            requirements.Add("digit");

        if (policy.RequireSpecialCharacter)
            requirements.Add("special character");

        return $"Password must contain {string.Join(", ", requirements)}";
    }
}
