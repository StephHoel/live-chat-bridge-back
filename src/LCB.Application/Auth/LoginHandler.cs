using LCB.Domain.Repositories;
using LCB.Application.Interfaces;

namespace LCB.Application.Auth;

public class LoginHandler
{
    private readonly IUserRepository _repo;
    private readonly ITokenService _tokenService;

    public LoginHandler(IUserRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }

    public async Task<string> Handle(LoginCommand command)
    {
        var user = await _repo.GetByEmail(command.Email);

        if (user == null)
            throw new Exception("Invalid credentials");

        return _tokenService.GenerateToken(user);
    }
}