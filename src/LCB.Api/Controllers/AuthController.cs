using LCB.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace LCB.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly LoginHandler _handler;

    public AuthController(LoginHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var token = await _handler.Handle(command);
        return Ok(new { token });
    }
}