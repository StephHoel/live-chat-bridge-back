namespace LCB.Api.Logging;

public sealed class TemplateLoggerProvider : ILoggerProvider
{
    private readonly LoggerExternalScopeProvider _scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
        => new TemplateLogger(categoryName, _scopeProvider);

    public void Dispose()
    {
        // nothing to dispose
    }
}
