using System.Globalization;

namespace AmarShowsBook.Helpers;

public static class CurrencyFormatter
{
    public static string FormatRupees(decimal? amount)
    {
        var value = amount ?? 0;
        var sign = value < 0 ? "-" : string.Empty;

        return $"{sign}₹ {Math.Abs(value).ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
