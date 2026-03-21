using System.Net;
using LCB.Domain.Extensions;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using LCB.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Login;

public class LoginHandler(IUserRepository repository, ITokenService tokenService, ILogger<LoginHandler> logger)
{
    public async Task<Result<LoginResponse>> Handle(LoginRequest request)
    {
        logger.LogInformationWithMethod("Login attempt for email {Email}.", new object?[] { request.Email });

        var user = await repository.GetByEmail(request.Email);

        if (user == null)
        {
            logger.LogWarningWithMethod("User not found for email {Email}.", new object?[] { request.Email });
            return Result<LoginResponse>.Fail("User not found", HttpStatusCode.NotFound);
        }

        var token = tokenService.GenerateToken(user);

        var response = new LoginResponse(token);

        logger.LogInformationWithMethod("User {UserId} authenticated successfully.", new object?[] { user.Id });

        return Result<LoginResponse>.Ok(response);
    }
}