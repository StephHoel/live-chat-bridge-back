using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories.Base;

/// <summary>
/// Base class providing common read/write helpers for in-memory repositories.
/// Centralizes logging, exception handling and locking/snapshot semantics.
/// </summary>
public abstract class InMemoryRepositoryBase<T>(ILogger Logger)
{
    protected readonly List<T> Items = [];
    protected readonly object Lock = new();

    /// <summary>
    /// Performs a read operation: creates a snapshot under lock and runs <paramref name="work"/> against it.
    /// </summary>
    protected Task<TResult> Read<TResult>(Func<IReadOnlyList<T>, TResult> work, string methodName)
    {
        Logger.LogInformation("[{method}] Starting...", methodName);
        try
        {
            List<T> snapshot;
            lock (Lock)
            {
                snapshot = [.. Items];
            }

            var result = work(snapshot);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{method}] Error", methodName);
            return Task.FromResult(default(TResult)!);
        }
        finally
        {
            Logger.LogInformation("[{method}] Finishing...", methodName);
        }
    }

    /// <summary>
    /// Performs a write operation: executes <paramref name="work"/> inside the lock.
    /// The work function should be synchronous and avoid long-running/blocking operations.
    /// </summary>
    protected Task<TResult> Write<TResult>(Func<List<T>, TResult> work, string methodName)
    {
        Logger.LogInformation("[{method}] Starting...", methodName);
        try
        {
            TResult result;
            lock (Lock)
            {
                result = work(Items);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{method}] Error", methodName);
            return Task.FromResult(default(TResult)!);
        }
        finally
        {
            Logger.LogInformation("[{method}] Finishing...", methodName);
        }
    }
}
