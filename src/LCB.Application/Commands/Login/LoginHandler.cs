using System.Net;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Domain.Objects;
using Microsoft.Extensions.Logging;

namespace LCB.Application.Commands.Login;

public class LoginHandler(IUserRepository repository, ITokenService tokenService, ILogger<LoginHandler> logger)
{
    public async Task<Result<LoginResponse>> Handle(LoginRequest request)
    {
        try
        {
            logger.LogInformation("Starting login attempt for email {Email}", [request.Email]);

            var user = await repository.GetByEmail(request.Email);

            if (user is null)
            {
                logger.LogWarning("User not found for email {Email}", [request.Email]);
                return Result<LoginResponse>.Fail("User not found", HttpStatusCode.NotFound);
            }

            var token = tokenService.GenerateToken(user);

            if (token is null)
            {
                logger.LogWarning("Token not generated");
                return Result<LoginResponse>.Fail("Fail on JWT generation", HttpStatusCode.InternalServerError);
            }

            logger.LogInformation("User authenticated successfully");

            var response = new LoginResponse(token);

            return Result<LoginResponse>.Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError("Error unexpected | Message: {Message} | StackTrace: {Stack}", [e.Message, e.StackTrace]);
            return Result<LoginResponse>.Fail("Error unexpected", HttpStatusCode.InternalServerError);
        }
        finally
        {
            logger.LogInformation("Finishing login attempt for email {Email}", [request.Email]);
        }
    }
}