using System.Net;
using System.Net.Mail;
using LCB.Application.Helpers;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Register;

public class RegisterHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    ILogger<RegisterHandler> logger)
{
    private const int MinPasswordLength = 8;

    public Task<Result<RegisterResponse>> Handle(RegisterRequest request)
        => OperationExecutor.ExecuteAsync(logger, nameof(RegisterHandler), () => ExecuteAsync(request));

    private async Task<Result<RegisterResponse>> ExecuteAsync(RegisterRequest request)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (!IsValidEmail(normalizedEmail))
            return Result<RegisterResponse>.Fail("Invalid email format", HttpStatusCode.BadRequest);

        if (!IsPasswordValid(request.Password))
            return Result<RegisterResponse>.Fail(
                "Password must be at least 8 characters and include uppercase, lowercase, number, and special character",
                HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            return Result<RegisterResponse>.Fail("Confirm password is required", HttpStatusCode.BadRequest);

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            return Result<RegisterResponse>.Fail("Passwords do not match", HttpStatusCode.BadRequest);

        var existingUser = await repository.GetByEmailAsync(normalizedEmail);
        if (existingUser is not null)
            return Result<RegisterResponse>.Fail("Email already registered", HttpStatusCode.Conflict);

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = UserEntity.Create(normalizedEmail, passwordHash);
        var created = await repository.CreateAsync([user]);

        if (!created)
        {
            // Handle race condition where another request persisted the same email first.
            var duplicatedUser = await repository.GetByEmailAsync(normalizedEmail);
            if (duplicatedUser is not null)
                return Result<RegisterResponse>.Fail("Email already registered", HttpStatusCode.Conflict);

            logger.LogWarning("Registration failed for email {Email}", normalizedEmail);
            return Result<RegisterResponse>.Fail("Could not create account", HttpStatusCode.InternalServerError);
        }

        logger.LogInformation("User registered successfully: {Email}", normalizedEmail);

        return Result<RegisterResponse>.Ok(
            new RegisterResponse("Account created successfully", normalizedEmail));
    }

    private static string NormalizeEmail(string email)
        => string.IsNullOrWhiteSpace(email)
            ? string.Empty
            : email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPasswordValid(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            return false;

        var hasUppercase = password.Any(char.IsUpper);
        var hasLowercase = password.Any(char.IsLower);
        var hasNumber = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUppercase && hasLowercase && hasNumber && hasSpecial;
    }
}