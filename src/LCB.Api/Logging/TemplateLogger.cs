namespace LCB.Api.Logging;

internal sealed class TemplateLogger : ILogger
{
    private readonly string _category;
    private readonly LoggerExternalScopeProvider _scopeProvider;
    private static readonly object _consoleLock = new();

    public TemplateLogger(string category, LoggerExternalScopeProvider scopeProvider)
    {
        _category = category;
        _scopeProvider = scopeProvider;
    }

    IDisposable ILogger.BeginScope<TState>(TState state)
        => _scopeProvider.Push(state!);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter == null) return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var level = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "NON",
        };
        string? correlationId = null;

        _scopeProvider.ForEachScope((scope, _) =>
        {
            switch (scope)
            {
                case IEnumerable<KeyValuePair<string, object>> kvps:
                    foreach (var kv in kvps)
                    {
                        if (kv.Key == "CorrelationId") correlationId = kv.Value?.ToString();
                    }
                    break;
                case KeyValuePair<string, object> kv:
                    if (kv.Key == "CorrelationId") correlationId = kv.Value?.ToString();
                    break;
                case string s:
                    // some scopes are pushed as simple strings
                    break;
            }
        }, state);

        var message = formatter(state, exception);

        var exceptionPart = exception is null ? string.Empty : $" | {exception.GetType().FullName}: {exception.Message}";

        string shortCategory = GetShortCategory();

        var line = $"[{timestamp} {level}] [CorrelationId: {correlationId ?? "none"}] [{shortCategory}] {message}{exceptionPart}";

        lock (_consoleLock)
        {
            Console.Out.WriteLine(line);
        }
    }

    private string GetShortCategory()
    {
        var shortCategory = _category;

        var lastDot = _category?.LastIndexOf('.') ?? -1;

        if (lastDot > 0 && lastDot < _category!.Length - 1)
            shortCategory = _category![(lastDot + 1)..];

        return shortCategory;
    }
}