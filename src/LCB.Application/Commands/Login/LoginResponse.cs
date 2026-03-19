namespace LCB.Application.Commands.Login;

public class LoginResponse(string? token = null)
{
    public string? Token { get; set; } = token;
}