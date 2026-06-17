using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories.Base;

/// <summary>
/// Base class providing common read/write helpers for in-memory repositories.
/// Centralizes logging, exception handling and locking/snapshot semantics.
/// </summary>
public abstract class InMemoryRepositoryBase<T>(ILogger Logger) : RepositoryBase(Logger)
{
    protected readonly List<T> Items = [];
    protected readonly object Lock = new();

    /// <summary>
    /// Performs a read operation: creates a snapshot under lock and runs <paramref name="work"/> against it.
    /// </summary>
    protected Task<TResult> Read<TResult>(Func<IReadOnlyList<T>, TResult> work, string methodName)
        => ExecuteAsync(() =>
        {
            List<T> snapshot;

            lock (Lock)
            {
                snapshot = [.. Items];
            }

            return Task.FromResult(work(snapshot));
        }, methodName);

    /// <summary>
    /// Performs a write operation: executes <paramref name="work"/> inside the lock.
    /// The work function should be synchronous and avoid long-running/blocking operations.
    /// </summary>
    protected Task<TResult> Write<TResult>(Func<List<T>, TResult> work, string methodName)
        => ExecuteAsync(() =>
        {
            lock (Lock)
            {
                return Task.FromResult(work(Items));
            }
        }, methodName);
}
