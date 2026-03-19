using System.Net;
using LCB.Application.Interfaces;
using LCB.Domain.Objects;
using LCB.Domain.Repositories;

namespace LCB.Application.Commands.Login;

public class LoginHandler(IUserRepository repository, ITokenService tokenService)
{
    public async Task<Result<LoginResponse>> Handle(LoginRequest request)
    {
        var user = await repository.GetByEmail(request.Email);

        if (user == null)
            return Result<LoginResponse>.Fail("User not found", HttpStatusCode.NotFound);

        var token = tokenService.GenerateToken(user);

        var response = new LoginResponse(token);

        return Result<LoginResponse>.Ok(response);
    }
}