namespace LCB.Application.Commands.Recover;

public class RecoverResponse(string message, string? temporaryResetToken = null)
{
    public string Message { get; set; } = message;
    public string? TemporaryResetToken { get; set; } = temporaryResetToken;
}
