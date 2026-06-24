namespace LCB.Domain.Models.Config;

public record PasswordPolicy
{
    public int MinLength { get; init; }
    public bool RequireUppercase { get; init; }
    public bool RequireLowercase { get; init; }
    public bool RequireDigit { get; init; }
    public bool RequireSpecialCharacter { get; init; }
}
