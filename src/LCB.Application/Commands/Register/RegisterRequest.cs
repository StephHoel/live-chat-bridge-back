namespace LCB.Application.Commands.Register;

public record RegisterRequest(string Email, string Password, string ConfirmPassword);