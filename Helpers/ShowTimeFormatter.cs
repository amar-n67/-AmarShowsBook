using System.Globalization;

namespace AmarShowsBook.Helpers;

public static class ShowTimeFormatter
{
    private static readonly TimeZoneInfo AppTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

    public static DateTime ToAppLocal(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return TimeZoneInfo.ConvertTime(value, AppTimeZone);
        }

        var utcValue = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(utcValue, AppTimeZone),
            DateTimeKind.Unspecified);
    }

    public static DateTime? ToAppLocal(DateTime? value) =>
        value.HasValue ? ToAppLocal(value.Value) : null;

    public static string Format(DateTime value, string format) =>
        ToAppLocal(value).ToString(format, CultureInfo.InvariantCulture);

    public static string Format(DateTime? value, string format, string fallback = "TBA") =>
        value.HasValue ? Format(value.Value, format) : fallback;

    public static string InputValue(DateTime value) =>
        ToAppLocal(value).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    public static long SortTicks(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTimeOffset(utcValue).ToUnixTimeMilliseconds();
    }
}
