using AmarShowsBook.Data;
using AmarShowsBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Filters;

// Keeps users inside their own booking flow: draft pages, QR payment, and confirmation must match the session user.
public class BookingStepValidationFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;

    public BookingStepValidationFilter(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString() ?? "";

        if (!string.Equals(controller, "Booking", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (IsPublicBookingEndpoint(action))
        {
            await next();
            return;
        }

        var userId = GetUserId(context.HttpContext.Session.GetString("UserId"));

        if (userId == null)
        {
            context.Result = LoginRedirect();
            return;
        }

        var validation = await ValidateBookingStep(context, action, userId.Value);

        if (validation == null)
        {
            await next();
            return;
        }

        context.Result = validation;
    }

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

    private static bool IsPublicBookingEndpoint(string action)
    {
        return action.Equals("CreateQR", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("Confirmation", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("MobilePay", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("ApprovePayment", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("RejectPayment", StringComparison.OrdinalIgnoreCase) ||
               action.Equals("TicketByBooking", StringComparison.OrdinalIgnoreCase);
    }

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

    private static int? GetUserId(string? value)
    {
        return int.TryParse(value, out var userId) ? userId : null;
    }

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

    private static string? GetString(ActionExecutingContext context, string key)
    {
        return context.ActionArguments.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }

    private static T? GetValue<T>(ActionExecutingContext context, string key)
    {
        return context.ActionArguments.TryGetValue(key, out var value)
            ? value is T typed ? typed : default
            : default;
    }

    private static IActionResult LoginRedirect()
    {
        return new RedirectToActionResult("Login", "Auth", null);
    }

    private static IActionResult InvalidStep()
    {
        return new RedirectToActionResult("ShowTime", "Home", null);
    }
}
