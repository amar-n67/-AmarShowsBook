using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Filters;

// Keeps users inside their own booking flow: draft pages, QR payment, and confirmation must match the session user.
public class BookingStepValidationFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogger _activityLogger;

    public BookingStepValidationFilter(
        ApplicationDbContext context,
        IActivityLogger activityLogger)
    {
        // Database checks confirm the draft/payment token belongs to the current session user.
        _context = context;
        // Blocked booking steps are logged here because the normal action logger does not run on early redirects.
        _activityLogger = activityLogger;
    }

    // Runs before Booking actions and stops users from jumping into another user's draft/payment flow.
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString() ?? "";

        if (!string.Equals(controller, "Booking", StringComparison.OrdinalIgnoreCase))
        {
            // Non-booking controllers are not part of the draft/payment step chain.
            await next();
            return;
        }

        if (IsPublicBookingEndpoint(action))
        {
            // Public booking endpoints perform their own checks because they support QR/mobile ticket links.
            await next();
            return;
        }

        var userId = GetUserId(context.HttpContext.Session.GetString("UserId"));

        if (userId == null)
        {
            await LogBlockedStep(
                context,
                action,
                null,
                "Booking step blocked because login is required.",
                "BOOKING_STEP_LOGIN_REQUIRED");

            context.Result = LoginRedirect();
            return;
        }

        var validation = await ValidateBookingStep(context, action, userId.Value);

        if (validation == null)
        {
            // A null validation result means the requested booking step is safe to run.
            await next();
            return;
        }

        await LogBlockedStep(
            context,
            action,
            userId.Value,
            "Booking step blocked because the draft, token, or owner check failed.",
            "BOOKING_STEP_DENIED");

        context.Result = validation;
    }

    // Chooses the exact validation rule needed for each booking action.
    private async Task<IActionResult?> ValidateBookingStep(
        ActionExecutingContext context,
        string action,
        long userId)
    {
        if (action.Equals("Details", StringComparison.OrdinalIgnoreCase))
        {
            var id = GetLong(context, "id");
            return await ValidateDraftOwner(id, userId, requirePending: true);
        }

        if (action.Equals("Payment", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("GenerateQR", StringComparison.OrdinalIgnoreCase))
        {
            var bookingId = GetLong(context, "bookingId");
            return await ValidateDraftOwner(bookingId, userId, requirePending: true);
        }

        if (action.Equals("Confirmation", StringComparison.OrdinalIgnoreCase))
        {
            var bookingId = GetLong(context, "bookingId");
            return await ValidateDraftOwner(bookingId, userId, requirePending: false);
        }

        if (action.Equals("MobilePay", StringComparison.OrdinalIgnoreCase))
        {
            var token = GetString(context, "token");
            return await ValidatePaymentToken(token, userId);
        }

        if (action.Equals("CompletePayment", StringComparison.OrdinalIgnoreCase))
        {
            var request = GetValue<PaymentRequest>(context, "request");
            return await ValidateDraftOwner(request?.BookingId, userId, requirePending: true);
        }

        if (action.Equals("ApprovePayment", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("RejectPayment", StringComparison.OrdinalIgnoreCase))
        {
            var token = GetString(context, "token");
            return await ValidatePaymentToken(token, userId);
        }

        if (action.Equals("CheckPaymentStatus", StringComparison.OrdinalIgnoreCase) ||
            action.Equals("CheckQRStatus", StringComparison.OrdinalIgnoreCase))
        {
            var bookingId = GetLong(context, "bookingId");
            return await ValidateDraftOwner(bookingId, userId, requirePending: false);
        }

        return null;
    }

    // These endpoints are opened for QR/payment/ticket flows and are validated inside their actions.
    private static bool IsPublicBookingEndpoint(string action)
    {
        return action.Equals("CreateQR", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("Confirmation", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("MobilePay", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("ApprovePayment", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("RejectPayment", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("TicketByBooking", StringComparison.OrdinalIgnoreCase);
    }

    // Confirms the draft exists, belongs to the user, and is still pending when the step requires it.
    private async Task<IActionResult?> ValidateDraftOwner(
        long? draftId,
        long userId,
        bool requirePending)
    {
        if (draftId == null)
        {
            return InvalidStep();
        }

        var draft = await _context.BookingDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == draftId.Value);

        if (draft == null || draft.UserId != userId)
        {
            return InvalidStep();
        }

        if (requirePending && !string.Equals(draft.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidStep();
        }

        return null;
    }

    // Confirms a QR/mobile payment token is still valid and linked to the user's own draft.
    private async Task<IActionResult?> ValidatePaymentToken(string? token, long userId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return InvalidStep();
        }

        var session = await _context.PaymentSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SessionToken == token);

        if (session == null ||
            session.ExpiresAt < DateTime.UtcNow ||
            !string.Equals(session.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            return InvalidStep();
        }

        var draft = await _context.BookingDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.BookingId);

        if (draft == null || draft.UserId != userId)
        {
            return InvalidStep();
        }

        return null;
    }

    // Reads the numeric user id from session safely.
    private static int? GetUserId(string? value)
    {
        return int.TryParse(value, out var userId) ? userId : null;
    }

    // Pulls a long route/form value out of action arguments without throwing.
    private static long? GetLong(ActionExecutingContext context, string key)
    {
        if (!context.ActionArguments.TryGetValue(key, out var value))
        {
            return null;
        }

        return value switch
        {
            long longValue => longValue,
            int intValue => intValue,
            string text when long.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }

    // Pulls a string route/form value out of action arguments.
    private static string? GetString(ActionExecutingContext context, string key)
    {
        return context.ActionArguments.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }

    // Reads complex action arguments such as PaymentRequest when MVC has already bound them.
    private static T? GetValue<T>(ActionExecutingContext context, string key)
    {
        return context.ActionArguments.TryGetValue(key, out var value)
            ? value is T typed ? typed : default
            : default;
    }

    // Sends unauthenticated users back to login before the booking action runs.
    private static IActionResult LoginRedirect()
    {
        return new RedirectToActionResult("Login", "Auth", null);
    }

    // Sends invalid or cross-user booking steps back to the public show list.
    private static IActionResult InvalidStep()
    {
        return new RedirectToActionResult("ShowTime", "Home", null);
    }

    // Records blocked booking navigation so suspicious or broken flows appear in Admin Activity Logs.
    private async Task LogBlockedStep(
        ActionExecutingContext context,
        string action,
        int? userId,
        string description,
        string errorCode)
    {
        var http = context.HttpContext;

        // The normal action logger does not run when this filter redirects early, so log the blocked step here.
        await _activityLogger.LogAsync(
            userId: userId,
            action: "BOOKING_STEP_BLOCKED",
            module: "BOOKING",
            entityType: "BOOKING_FLOW",
            description: description,
            status: "FAILURE",
            errorCode: errorCode,
            errorMessage: $"Blocked Booking/{action}",
            errorSource: nameof(BookingStepValidationFilter),
            isError: 1,
            metadata: new
            {
                action,
                path = http.Request.Path.ToString(),
                query = http.Request.QueryString.ToString(),
                route = context.RouteData.Values.ToDictionary(x => x.Key, x => x.Value?.ToString()),
                data = AuditValueSanitizer.SanitizeActionArguments(context.ActionArguments)
            });
    }
}
