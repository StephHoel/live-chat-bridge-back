using LCB.Domain.Interfaces.Services;

namespace LCB.Infrastructure.Services.Auth;

public class RecoverAntiAbuseService : IRecoverAntiAbuseService
{
    private readonly Lock sync = new();
    private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);

    public bool TryAcquire(string key, int maxAttempts, TimeSpan window)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (maxAttempts <= 0 || window <= TimeSpan.Zero)
            return false;

        var now = DateTimeOffset.UtcNow;

        lock (sync)
        {
            if (entries.TryGetValue(key, out var current) && current.ExpiresAtUtc <= now)
                entries.Remove(key);

            if (!entries.TryGetValue(key, out current))
            {
                entries[key] = new Entry(1, now.Add(window));
                return true;
            }

            if (current.Count >= maxAttempts)
                return false;

            entries[key] = current with { Count = current.Count + 1 };
            return true;
        }
    }

    private sealed record Entry(int Count, DateTimeOffset ExpiresAtUtc);
}
