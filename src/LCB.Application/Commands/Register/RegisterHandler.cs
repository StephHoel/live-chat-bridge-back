using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Entities;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using LCB.Domain.Services;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Register;

public class RegisterHandler(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    PasswordValidator passwordValidator,
    ILogger<RegisterHandler> logger)
{
    public Task<Result<RegisterResponse>> Handle(RegisterRequest request)
        => OperationExecutor.ExecuteAsync(logger, nameof(RegisterHandler), () => ExecuteAsync(request));

    private async Task<Result<RegisterResponse>> ExecuteAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.NormalizeEmail();

        if (!normalizedEmail.IsValidEmail())
            return Result<RegisterResponse>.Fail("Invalid email format", HttpStatusCode.BadRequest);

        if (!passwordValidator.IsPasswordValid(request.Password))
            return Result<RegisterResponse>.Fail(
                passwordValidator.GetPasswordErrorMessage(),
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
}