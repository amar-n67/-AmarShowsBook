using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace AmarShowsBook.Filters;

// Writes a simple audit row after each MVC action so admin activity pages can show who did what.
public class ActivityLoggingFilter : IAsyncActionFilter
{
    private readonly IActivityLogger _activityLogger;

    public ActivityLoggingFilter(IActivityLogger activityLogger)
    {
        // The shared logger writes to the database and also protects the app if logging fails.
        _activityLogger = activityLogger;
    }

    // Wraps every MVC action so the Activity Logs page can show what route ran and how it ended.
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "UNKNOWN";
        var action = context.RouteData.Values["action"]?.ToString() ?? "UNKNOWN";

        if (IsClientActivityEndpoint(controller, action))
        {
            // ClientEvent creates its own UI event row, so the wrapper request is skipped to avoid duplicates.
            await next();
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Run the action first so the log can include the real result, status, and exception details.
            var executed = await next();
            stopwatch.Stop();

            await WriteActionLog(context, executed, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // If MVC throws before it can build an ActionExecutedContext, still leave an audit trail.
            await WriteActionLog(context, null, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    // Writes the final audit row after a normal action, handled failure, or thrown exception.
    private async Task WriteActionLog(
        ActionExecutingContext context,
        ActionExecutedContext? executed,
        long elapsedMilliseconds,
        Exception? thrownException)
    {
        var http = context.HttpContext;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "UNKNOWN";
        var action = context.RouteData.Values["action"]?.ToString() ?? "UNKNOWN";
        var userId = TryGetUserId(http.Session.GetString("UserId"));
        var exception = thrownException ?? executed?.Exception;
        var statusCode = ResolveStatusCode(executed?.Result, http.Response.StatusCode, exception);
        var status = exception == null && statusCode < StatusCodes.Status400BadRequest
            ? "SUCCESS"
            : "FAILURE";
        var auditAction = ResolveAuditAction(http.Request.Method, executed?.Result, http.Request.Headers.Accept.ToString());

        // One row per MVC action gives admins a simple trail for pages, JSON calls, exports, and errors.
        await _activityLogger.LogAsync(
            userId: userId,
            action: auditAction,
            module: controller.ToUpperInvariant(),
            entityType: "MVC_ACTION",
            description: $"{http.Request.Method} {controller}/{action}",
            status: status,
            errorMessage: GetExceptionMessage(exception),
            errorSource: exception?.Source,
            stackTrace: exception?.StackTrace,
            isError: status == "SUCCESS" ? 0 : 1,
            metadata: new
            {
                controller,
                action,
                path = http.Request.Path.ToString(),
                query = http.Request.QueryString.ToString(),
                httpStatus = statusCode,
                elapsedMs = elapsedMilliseconds,
                resultType = executed?.Result?.GetType().Name ?? "None",
                exceptionHandled = executed?.ExceptionHandled ?? false,
                actionCanceled = executed?.Canceled ?? false,
                userEmail = http.Session.GetString("UserEmail"),
                userName = http.Session.GetString("UserName"),
                traceId = http.TraceIdentifier,
                request = new
                {
                    scheme = http.Request.Scheme,
                    host = http.Request.Host.ToString(),
                    method = http.Request.Method,
                    contentType = http.Request.ContentType,
                    accept = http.Request.Headers.Accept.ToString(),
                    referer = http.Request.Headers.Referer.ToString()
                },
                route = context.RouteData.Values.ToDictionary(x => x.Key, x => x.Value?.ToString()),
                data = AuditValueSanitizer.SanitizeActionArguments(context.ActionArguments)
            });
    }

    // Session UserId is optional because guests can browse public pages.
    private static int? TryGetUserId(string? value)
    {
        return int.TryParse(value, out var userId) ? userId : null;
    }

    // Keep top-level audit actions consistent, while specific controllers can still add business events.
    private static string ResolveAuditAction(string method, IActionResult? result, string acceptHeader)
    {
        // GET views are page views; GET JSON/files are treated as data fetches.
        if (HttpMethods.IsGet(method))
        {
            return IsDataResult(result, acceptHeader) ? "DATA_FETCH" : "PAGE_VIEW";
        }

        // POST/PUT/PATCH/DELETE calls usually create or change data, even when they end in a redirect.
        if (HttpMethods.IsPost(method) ||
            HttpMethods.IsPut(method) ||
            HttpMethods.IsPatch(method) ||
            HttpMethods.IsDelete(method))
        {
            return "DATA_ENTRY";
        }

        return "ACTION_EXECUTED";
    }

    // Identifies result types that behave like API/data responses instead of full page views.
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

    // MVC result objects often know the intended status before the response has finished writing.
    private static int ResolveStatusCode(IActionResult? result, int responseStatusCode, Exception? exception)
    {
        // A thrown exception is logged as 500 even if the response has not been written yet.
        if (exception != null)
        {
            return StatusCodes.Status500InternalServerError;
        }

        return result switch
        {
            ObjectResult objectResult when objectResult.StatusCode.HasValue => objectResult.StatusCode.Value,
            JsonResult jsonResult when jsonResult.StatusCode.HasValue => jsonResult.StatusCode.Value,
            BadRequestResult => StatusCodes.Status400BadRequest,
            UnauthorizedResult => StatusCodes.Status401Unauthorized,
            ChallengeResult => StatusCodes.Status401Unauthorized,
            ForbidResult => StatusCodes.Status403Forbidden,
            NotFoundResult => StatusCodes.Status404NotFound,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            FileResult => StatusCodes.Status200OK,
            RedirectResult or RedirectToActionResult or RedirectToRouteResult => StatusCodes.Status302Found,
            _ => responseStatusCode
        };
    }

    // Prevents duplicate audit rows for browser click/change logging.
    private static bool IsClientActivityEndpoint(string controller, string action)
    {
        return controller.Equals("Activity", StringComparison.OrdinalIgnoreCase) &&
               action.Equals("ClientEvent", StringComparison.OrdinalIgnoreCase);
    }

    // Flattens nested exception messages into a compact value for the admin activity table.
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
