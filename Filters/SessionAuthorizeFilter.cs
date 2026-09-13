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
            "RecoverDeletedAccount",
            "CloseApplication"
        };

    private readonly IActivityLogger _activityLogger;
    private readonly RbacService _rbacService;

    public SessionAuthorizeFilter(
        IActivityLogger activityLogger,
        RbacService rbacService)
    {
        // Keep logging and RBAC checks in this filter so every protected route follows the same gate.
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

        // Public pages must stay open so guests can browse shows, login, recover accounts, and use Amaro show help.
        if (IsPublicEndpoint(controller, action))
        {
            await next();
            return;
        }

        var userEmail = http.Session.GetString("UserEmail");

        // A session email means the user passed login; UserId is still checked because RBAC needs the numeric id.
        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            var userId =
            TryGetUserId(http.Session.GetString("UserId"));

            if (userId == null)
            {
                // Broken sessions are sent back through login instead of letting the route run without a user id.
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Auth",
                    new { returnUrl = http.Request.Path + http.Request.QueryString });
                return;
            }

            if (RequiresRoleAccess(controller, action, userId.Value, out var roleDeniedMessage) &&
                roleDeniedMessage != null)
            {
                // Role-level denials are logged before the user is redirected or receives API JSON.
                await _activityLogger.LogAsync(
                    userId: userId,
                    action: "RBAC_ROLE_ACCESS_DENIED",
                    module: controller.ToUpperInvariant(),
                    entityType: "ROUTE",
                    description: roleDeniedMessage,
                    status: "FAILURE",
                    isError: 1,
                    metadata: new
                    {
                        controller,
                        action,
                        path = http.Request.Path.ToString()
                    });

                if (IsAjaxOrApi(http.Request))
                {
                    // JavaScript/API callers need a 403 payload; browser page requests use a friendly redirect.
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        message = roleDeniedMessage
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

            // RBAC-denied API calls return 403 JSON; page requests go back to the customer showTime page.
            if (RequiresPermission(controller, action, out var moduleCode, out var actionType) &&
                !_rbacService.HasPermission(userId.Value, moduleCode, actionType))
            {
                // Permission denials include module/action details so Admin Activity Logs explain the block clearly.
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

        // No login session exists, so record the direct access attempt before any redirect happens.
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
            // API callers cannot follow a normal login redirect cleanly, so they get a 401 JSON response.
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

    // Lists routes that are intentionally available before login.
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
            return action.Equals("Intro", StringComparison.OrdinalIgnoreCase) ||
                   action.Equals("Index", StringComparison.OrdinalIgnoreCase) ||
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

    // Maps controller actions to the module/action permission used by RBAC.
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

    // Checks coarse role rules before fine-grained permission rules.
    private bool RequiresRoleAccess(
        string controller,
        string action,
        int userId,
        out string? deniedMessage)
    {
        deniedMessage = null;

        if (controller.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (action.Equals("Dashboard", StringComparison.OrdinalIgnoreCase) ||
                action.Equals("Index", StringComparison.OrdinalIgnoreCase))
            {
                if (!_rbacService.CanOpenAdminDashboard(userId))
                {
                    deniedMessage = "Only Administrator, Super Admin, or Developer can access the admin dashboard.";
                }

                return true;
            }

            if (IsSuperAdminAreaAction(action) &&
                !_rbacService.CanAccessSuperAdminArea(userId))
            {
                deniedMessage = "Only Super Admin or Developer can access this admin page.";
                return true;
            }

            if (IsExportAction(action) && !_rbacService.IsSuperAdmin(userId))
            {
                deniedMessage = "Only Super Admin can export data.";
                return true;
            }
        }

        if (controller.Equals("Booking", StringComparison.OrdinalIgnoreCase) &&
            action.Equals("DownloadTicket", StringComparison.OrdinalIgnoreCase) &&
            !_rbacService.IsSuperAdmin(userId))
        {
            deniedMessage = "Only Super Admin can download or print tickets.";
            return true;
        }

        return false;
    }

    // Super-admin pages can change roles, permissions, and managed show setup.
    private static bool IsSuperAdminAreaAction(string action)
    {
        return action.Equals("Roles", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("CreateRole", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("UpdateRole", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("Permissions", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("CreatePermission", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("ToggleRolePermission", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("ManageShows", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("CreateManagedShow", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("UpdateManagedShow", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("DeleteManagedShow", StringComparison.OrdinalIgnoreCase);
    }

    // Export actions are treated as sensitive because they can reveal large data sets.
    private static bool IsExportAction(string action)
    {
        return action.StartsWith("Export", StringComparison.OrdinalIgnoreCase);
    }

    // Session values are strings, so this helper safely converts UserId for RBAC.
    private static int? TryGetUserId(string? value)
    {
        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }

    // AJAX/API requests should receive JSON status responses instead of HTML redirects.
    private static bool IsAjaxOrApi(HttpRequest request)
    {
        return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               request.Headers.Accept.Any(x => x?.Contains("application/json") == true) ||
               request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true;
    }
}
