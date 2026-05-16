using System.Globalization;

namespace AmarShowsBook.Helpers;

public static class CurrencyFormatter
{
    // Human Comment:
    // Keeps rupee amounts consistent and places the negative sign before the rupee symbol.
    public static string FormatRupees(decimal? amount)
    {
        var value = amount ?? 0;
        var sign = value < 0 ? "-" : string.Empty;

        return $"{sign}₹ {Math.Abs(value).ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
