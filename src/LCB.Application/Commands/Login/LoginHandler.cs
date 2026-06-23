using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Login;

public class LoginHandler(
    IUserRepository repository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher,
    ILogger<LoginHandler> logger)
{
    public Task<Result<LoginResponse>> Handle(LoginRequest request)
        => OperationExecutor.ExecuteAsync(logger, nameof(LoginHandler), () => ExecuteAsync(request));

    private async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request)
    {
        var user = await repository.GetByEmailAsync(request.Email);

        if (user is null)
        {
            logger.LogWarning("Authentication failed: user not found for email {Email}", request.Email);
            return Result<LoginResponse>.Fail("Invalid email or password", HttpStatusCode.Unauthorized);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Authentication failed: invalid password for email {Email}", request.Email);
            return Result<LoginResponse>.Fail("Invalid email or password", HttpStatusCode.Unauthorized);
        }

        var token = tokenService.GenerateToken(user);

        if (token is null)
        {
            logger.LogWarning("Token generation failed for email {Email}", request.Email);
            return Result<LoginResponse>.Fail("Fail on JWT generation", HttpStatusCode.InternalServerError);
        }

        logger.LogInformation("User authenticated successfully: {Email}", request.Email);

        return Result<LoginResponse>.Ok(new LoginResponse(token));
    }
}