namespace LCB.Domain.Interfaces.Services;

public interface IRecoverAntiAbuseService
{
    bool TryAcquire(string key, int maxAttempts, TimeSpan window);
}
