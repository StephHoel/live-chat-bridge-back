using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace LCB.Domain.Extensions;

public static class LoggerExtensions
{
    public static void LogInformationWithMethod(this ILogger logger, string message, object?[]? args = null, [CallerMemberName] string memberName = "")
    {
        using (logger.BeginScope(new Dictionary<string, object?> { ["Method"] = memberName }))
        {
            if (args == null) logger.LogInformation(message);
            else logger.LogInformation(message, args);
        }
    }

    public static void LogWarningWithMethod(this ILogger logger, string message, object?[]? args = null, [CallerMemberName] string memberName = "")
    {
        using (logger.BeginScope(new Dictionary<string, object?> { ["Method"] = memberName }))
        {
            if (args == null) logger.LogWarning(message);
            else logger.LogWarning(message, args);
        }
    }

    public static void LogDebugWithMethod(this ILogger logger, string message, object?[]? args = null, [CallerMemberName] string memberName = "")
    {
        using (logger.BeginScope(new Dictionary<string, object?> { ["Method"] = memberName }))
        {
            if (args == null) logger.LogDebug(message);
            else logger.LogDebug(message, args);
        }
    }
}
