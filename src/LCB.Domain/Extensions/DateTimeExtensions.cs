namespace LCB.Domain.Extensions;

public static class DateTimeExtensions
{
    public static readonly TimeSpan UtcMinus3Offset = TimeSpan.FromHours(-3);

    public static DateTime NormalizeToUtcMinus3(this DateTime timestamp)
    {
        var asOffset = timestamp.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(timestamp, TimeSpan.Zero),
            DateTimeKind.Local => new DateTimeOffset(timestamp.ToUniversalTime(), TimeSpan.Zero),
            _ => new DateTimeOffset(DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified), UtcMinus3Offset)
        };

        return asOffset.ToOffset(UtcMinus3Offset).DateTime;
    }

    public static DateTime NowUtcMinus3()
        => DateTime.UtcNow.NormalizeToUtcMinus3();

    public static DateTimeOffset AsUtcMinus3Offset(this DateTime timestamp)
        => new(DateTime.SpecifyKind(timestamp.NormalizeToUtcMinus3(), DateTimeKind.Unspecified), UtcMinus3Offset);

    public static DateTime ToDateTime(this long timeStamp)
        => DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).UtcDateTime;
}