using System.Net;
using LCB.Application.Helpers;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Login;

public class LoginHandler(IUserRepository repository, ITokenService tokenService, ILogger<LoginHandler> logger)
{
    public Task<Result<LoginResponse>> Handle(LoginRequest request)
        => OperationExecutor.ExecuteAsync(logger, nameof(LoginHandler), () => ExecuteAsync(request));

    private async Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request)
    {
        var user = await repository.GetByEmailAsync(request.Email);

        if (user is null)
        {
            logger.LogWarning("User not found for email {Email}", request.Email);
            return Result<LoginResponse>.Fail("User not found", HttpStatusCode.NotFound);
        }

        var token = tokenService.GenerateToken(user);

        if (token is null)
        {
            logger.LogWarning("Token generation failed");
            return Result<LoginResponse>.Fail("Fail on JWT generation", HttpStatusCode.InternalServerError);
        }

        logger.LogInformation("User authenticated successfully");

        return Result<LoginResponse>.Ok(new LoginResponse(token));
    }
}