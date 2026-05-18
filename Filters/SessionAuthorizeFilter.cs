using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmarShowsBook.Filters;

public class SessionAuthorizeFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> PublicAuthActions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Login",
            "Signup",
            "ForgotPassword",
            "SendOTP",
            "VerifyOTP",
            "ResetPassword",
            "CloseApplication"
        };

    private readonly IActivityLogger _activityLogger;

    public SessionAuthorizeFilter(IActivityLogger activityLogger)
    {
        _activityLogger = activityLogger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        var http = context.HttpContext;

        if (IsPublicEndpoint(controller, action))
        {
            await next();
            return;
        }

        var userEmail = http.Session.GetString("UserEmail");

        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            await next();
            return;
        }

        await _activityLogger.LogAsync(
            userId: null,
            action: "UNAUTHORIZED_DIRECT_ACCESS",
            module: controller.ToUpperInvariant(),
            entityType: "ROUTE",
            description: $"Blocked unauthenticated access to {controller}/{action}",
            status: "FAILURE",
            isError: 1,
            metadata: new
            {
                controller,
                action,
                path = http.Request.Path.ToString(),
                query = http.Request.QueryString.ToString()
            });

        if (IsAjaxOrApi(http.Request))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                message = "Login required"
            });
            return;
        }

        context.Result = new RedirectToActionResult(
            "Login",
            "Auth",
            new { returnUrl = http.Request.Path + http.Request.QueryString });
    }

    private static bool IsPublicEndpoint(string controller, string action)
    {
        if (controller.Equals("Auth", StringComparison.OrdinalIgnoreCase))
        {
            return PublicAuthActions.Contains(action) ||
                   action.Equals("Logout", StringComparison.OrdinalIgnoreCase);
        }

        if (controller.Equals("Otp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return controller.Equals("Home", StringComparison.OrdinalIgnoreCase) &&
               action.Equals("Error", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAjaxOrApi(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               request.Headers.Accept.Any(x => x?.Contains("application/json") == true) ||
               request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }
}
