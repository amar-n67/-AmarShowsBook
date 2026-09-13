using System.Collections;

namespace AmarShowsBook.Services;

// Keeps audit metadata useful while preventing passwords, OTPs, cards, and tokens from entering activity logs.
public static class AuditValueSanitizer
{
    // Keep the argument names because they tell admins which route/form field was involved.
    public static Dictionary<string, object?> SanitizeActionArguments(IDictionary<string, object?> arguments)
    {
        return arguments.ToDictionary(
            item => item.Key,
            item => SanitizeValue(item.Key, item.Value, 0));
    }

    // Walk only a few levels into models so logging cannot become slow or accidentally serialize huge graphs.
    private static object? SanitizeValue(string key, object? value, int depth)
    {
        if (ShouldRedact(key))
        {
            return "[REDACTED]";
        }

        if (value == null || depth > 2)
        {
            return value == null ? null : "[OBJECT]";
        }

        if (value is string ||
            value.GetType().IsPrimitive ||
            value is decimal ||
            value is DateTime ||
            value is DateOnly ||
            value is TimeOnly ||
            value is Guid)
        {
            return value;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            return enumerable
                .Cast<object?>()
                .Take(20)
                .Select(item => SanitizeValue(key, item, depth + 1))
                .ToList();
        }

        return value.GetType()
            .GetProperties()
            .Where(property => property.GetIndexParameters().Length == 0)
            .Take(40)
            .ToDictionary(
                property => property.Name,
                property => SanitizeValue(property.Name, property.GetValue(value), depth + 1));
    }

    private static bool ShouldRedact(string key)
    {
        var normalized = key.ToLowerInvariant();
        return normalized.Contains("password") ||
               normalized.Contains("otp") ||
               normalized.Contains("token") ||
               normalized.Contains("secret") ||
               normalized.Contains("authorization") ||
               normalized.Contains("cvv") ||
               normalized.Contains("card") ||
               normalized.Contains("pin");
    }
}
