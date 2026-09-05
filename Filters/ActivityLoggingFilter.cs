using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections;

namespace AmarShowsBook.Filters;

// Writes a simple audit row after each MVC action so admin activity pages can show who did what.
public class ActivityLoggingFilter : IAsyncActionFilter
{
    private readonly IActivityLogger _activityLogger;

    public ActivityLoggingFilter(IActivityLogger activityLogger)
    {
        _activityLogger = activityLogger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var executed = await next();
        var http = context.HttpContext;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "UNKNOWN";
        var action = context.RouteData.Values["action"]?.ToString() ?? "UNKNOWN";
        var userId = TryGetUserId(http.Session.GetString("UserId"));
        var status = executed.Exception == null ? "SUCCESS" : "FAILURE";
        var errorMessage = GetExceptionMessage(executed.Exception);
        var statusCode = ResolveStatusCode(executed.Result, http.Response.StatusCode);
        var auditAction = ResolveAuditAction(http.Request.Method, executed.Result, http.Request.Headers.Accept.ToString());

        await _activityLogger.LogAsync(
            userId: userId,
            action: auditAction,
            module: controller.ToUpperInvariant(),
            entityType: "MVC_ACTION",
            description: $"{http.Request.Method} {controller}/{action}",
            status: status,
            errorMessage: errorMessage,
            errorSource: executed.Exception?.Source,
            stackTrace: executed.Exception?.StackTrace,
            isError: executed.Exception == null ? 0 : 1,
            metadata: new
            {
                controller,
                action,
                path = http.Request.Path.ToString(),
                query = http.Request.QueryString.ToString(),
                httpStatus = statusCode,
                resultType = executed.Result?.GetType().Name ?? "None",
                route = context.RouteData.Values.ToDictionary(x => x.Key, x => x.Value?.ToString()),
                data = SanitizeActionArguments(context.ActionArguments)
            });
    }

    private static int? TryGetUserId(string? value)
    {
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private static string ResolveAuditAction(string method, IActionResult? result, string acceptHeader)
    {
        if (HttpMethods.IsGet(method))
        {
            return IsDataResult(result, acceptHeader) ? "DATA_FETCH" : "PAGE_VIEW";
        }

        if (HttpMethods.IsPost(method) ||
            HttpMethods.IsPut(method) ||
            HttpMethods.IsPatch(method) ||
            HttpMethods.IsDelete(method))
        {
            return "DATA_ENTRY";
        }

        return "ACTION_EXECUTED";
    }

    private static bool IsDataResult(IActionResult? result, string acceptHeader)
    {
        if (result is JsonResult ||
            result is ObjectResult ||
            result is FileResult ||
            result is StatusCodeResult ||
            result is ContentResult)
        {
            return true;
        }

        return acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveStatusCode(IActionResult? result, int responseStatusCode)
    {
        return result switch
        {
            ObjectResult objectResult when objectResult.StatusCode.HasValue => objectResult.StatusCode.Value,
            JsonResult jsonResult when jsonResult.StatusCode.HasValue => jsonResult.StatusCode.Value,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            FileResult => StatusCodes.Status200OK,
            RedirectResult or RedirectToActionResult or RedirectToRouteResult => StatusCodes.Status302Found,
            _ => responseStatusCode
        };
    }

    private static Dictionary<string, object?> SanitizeActionArguments(IDictionary<string, object?> arguments)
    {
        return arguments.ToDictionary(
            item => item.Key,
            item => SanitizeValue(item.Key, item.Value, 0));
    }

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
               normalized.Contains("cvv") ||
               normalized.Contains("card") ||
               normalized.Contains("pin");
    }

    private static string? GetExceptionMessage(Exception? exception)
    {
        if (exception == null)
        {
            return null;
        }

        var messages = new List<string>();

        for (var current = exception; current != null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                messages.Add(current.Message);
            }
        }

        return string.Join(" | ", messages.Distinct());
    }
}
