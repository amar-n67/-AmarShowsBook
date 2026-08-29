using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmarShowsBook.Filters;

// Runs before controller actions to require login and block role-protected routes early.
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
    private readonly RbacService _rbacService;

    public SessionAuthorizeFilter(
        IActivityLogger activityLogger,
        RbacService rbacService)
    {
        _activityLogger = activityLogger;
        _rbacService = rbacService;
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
            var userId =
            TryGetUserId(http.Session.GetString("UserId"));

            // RBAC-denied API calls return 403 JSON; page requests go back to the customer showTime page.
            if (RequiresPermission(controller, action, out var moduleCode, out var actionType) &&
                (userId == null || !_rbacService.HasPermission(userId.Value, moduleCode, actionType)))
            {
                await _activityLogger.LogAsync(
                    userId: userId,
                    action: "RBAC_ACCESS_DENIED",
                    module: moduleCode,
                    entityType: "ROUTE",
                    description: $"Blocked role access to {controller}/{action}",
                    status: "FAILURE",
                    isError: 1,
                    metadata: new
                    {
                        controller,
                        action,
                        moduleCode,
                        actionType,
                        path = http.Request.Path.ToString()
                    });

                if (IsAjaxOrApi(http.Request))
                {
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        message = "You do not have access to this action."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }

                context.Result = new RedirectToActionResult(
                    "ShowTime",
                    "Home",
                    new { accessDenied = true });
                return;
            }

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

        if (controller.Equals("Booking", StringComparison.OrdinalIgnoreCase))
        {
            return action.Equals("CreateQR", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("Confirmation", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("MobilePay", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("ApprovePayment", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("RejectPayment", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("TicketByBooking", StringComparison.OrdinalIgnoreCase);
        }

        if (controller.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            return action.Equals("Index", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("ShowTime", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("News", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("ResolveNewsLive", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("GetCountries", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("GetStates", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("GetDistricts", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("ShowDates", StringComparison.OrdinalIgnoreCase);
        }

        return controller.Equals("Amaro", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresPermission(
        string controller,
        string action,
        out string moduleCode,
        out string actionType)
    {
        moduleCode = "";
        actionType = "";

        if (controller.Equals("Developer", StringComparison.OrdinalIgnoreCase))
        {
            moduleCode = "DEVELOPER";
            actionType = "EDIT";
            return true;
        }

        if (!controller.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        (moduleCode, actionType) = action switch
        {
            "Dashboard" or "Index" => ("ADMIN", "VIEW"),
            "Users" or "UserDetails" => ("USER", "VIEW"),
            "UserAccess" or "AddUserRole" or "RemoveUserRole" => ("USER", "GRANT_ACCESS"),
            "ToggleUserStatus" or "DeleteUser" => ("USER", "DISABLE"),
            "Roles" => ("ROLE", "VIEW"),
            "CreateRole" => ("ROLE", "CREATE"),
            "UpdateRole" => ("ROLE", "UPDATE"),
            "Permissions" => ("PERMISSION", "VIEW"),
            "CreatePermission" => ("PERMISSION", "CREATE"),
            "ToggleRolePermission" => ("PERMISSION", "ASSIGN"),
            "ManageShows" or "Bookings" => ("BOOKING", "VIEW"),
            "Transactions" or "TransactionDetails" => ("PAYMENT", "VIEW"),
            "Refunds" or "RefundDetails" => ("REFUND", "VIEW"),
            "ApproveRefund" => ("REFUND", "APPROVE"),
            "RejectRefund" => ("REFUND", "REJECT"),
            "RetryRefund" => ("REFUND", "RETRY"),
            "Wallets" => ("WALLET", "VIEW"),
            "CouponUsage" => ("COUPON", "VIEW"),
            "Notifications" => ("NOTIFICATION", "VIEW"),
            "Security" or "AcknowledgeSecurityAlerts" or "AddSecurityValidation" or "ClearSecurityAlert" or "BlockTicketFromSecurity" or "RegisterScannerDevice" => ("SCANNER", "VIEW"),
            "ActivityLogs" or "Versions" => ("ANALYTICS", "VIEW"),
            _ => ("ADMIN", "VIEW")
        };

        return true;
    }

    private static int? TryGetUserId(string? value)
    {
        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }

    private static bool IsAjaxOrApi(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               request.Headers.Accept.Any(x => x?.Contains("application/json") == true) ||
               request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }
}
