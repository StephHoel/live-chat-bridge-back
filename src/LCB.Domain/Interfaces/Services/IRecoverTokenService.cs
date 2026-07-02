namespace LCB.Domain.Interfaces.Services;

public interface IRecoverTokenService
{
    string GenerateTemporaryResetToken();
}
