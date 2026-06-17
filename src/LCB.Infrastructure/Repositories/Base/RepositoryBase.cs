using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories.Base;

/// <summary>
/// Base class providing common execution helpers for repositories.
/// Centralizes logging, exception handling and finish logging.
/// </summary>
public abstract class RepositoryBase(ILogger Logger)
{
    protected Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> work, string methodName)
        => ExecuteAsyncInternal(work, methodName);

    protected Task ExecuteAsync(Func<Task> work, string methodName)
        => ExecuteAsyncInternal(async () =>
        {
            await work();
            return true;
        }, methodName);

    private async Task<TResult> ExecuteAsyncInternal<TResult>(Func<Task<TResult>> work, string methodName)
    {
        Logger.LogInformation("[{method}] Starting...", methodName);

        try
        {
            return await work();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{method}] Error", methodName);
            return default!;
        }
        finally
        {
            Logger.LogInformation("[{method}] Finishing...", methodName);
        }
    }
}