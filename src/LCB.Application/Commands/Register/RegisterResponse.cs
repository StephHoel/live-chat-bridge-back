namespace LCB.Application.Commands.Register;

public class RegisterResponse(string message, string email)
{
    public string Message { get; set; } = message;
    public string Email { get; set; } = email;
}