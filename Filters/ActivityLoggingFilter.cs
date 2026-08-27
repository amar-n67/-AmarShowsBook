using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc.Filters;

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

        await _activityLogger.LogAsync(
            userId: userId,
            action: http.Request.Method == "GET" ? "PAGE_VIEW" : "ACTION_EXECUTED",
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
                route = context.RouteData.Values.ToDictionary(x => x.Key, x => x.Value?.ToString())
            });
    }

    private static int? TryGetUserId(string? value)
    {
        return int.TryParse(value, out var userId) ? userId : null;
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
