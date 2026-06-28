namespace LCB.Application.Helpers;

public static class LiveUsernameNormalizer
{
    public static string? Normalize(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var value = rawValue.Trim();

        var pathCandidate = TryExtractPathFromUrl(value);
        if (!string.IsNullOrWhiteSpace(pathCandidate))
            value = pathCandidate!;

        value = value.Trim();
        value = value.Trim('/');

        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.TrimStart('@').Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (value.Contains('/'))
        {
            var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            value = parts.FirstOrDefault(x => x.StartsWith('@'))
                ?? parts.FirstOrDefault()
                ?? string.Empty;

            value = value.TrimStart('@').Trim();
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? TryExtractPathFromUrl(string value)
    {
        if (TryParseAbsoluteUri(value, out var absoluteUri))
            return ExtractHandlePath(absoluteUri);

        if (!value.Contains('/', StringComparison.Ordinal))
            return null;

        if (TryParseAbsoluteUri($"https://{value}", out var uriWithScheme))
            return ExtractHandlePath(uriWithScheme);

        return null;
    }

    private static bool TryParseAbsoluteUri(string value, out Uri uri)
        => Uri.TryCreate(value, UriKind.Absolute, out uri!)
           && !string.IsNullOrWhiteSpace(uri.Host);

    private static string? ExtractHandlePath(Uri uri)
    {
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
            return null;

        var segment = segments.FirstOrDefault(x => x.StartsWith('@'))
                      ?? segments.FirstOrDefault();

        return segment;
    }
}
