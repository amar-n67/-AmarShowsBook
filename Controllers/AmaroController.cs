using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers;

public class AmaroController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly RbacService _rbacService;

    public AmaroController(
        ApplicationDbContext context,
        RbacService rbacService)
    {
        _context = context;
        _rbacService = rbacService;
    }

    [HttpGet]
    public async Task<IActionResult> Menu()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Json(new
            {
                isLoggedIn = false,
                greeting = "Hey guest, I'm Amaro. Please login so I can help with your account.",
                options = new[] { "Login", "Signup", "Browse shows" }
            });
        }

        var menuItems = await GetAccessibleMenus(userId);

        return Json(new
        {
            isLoggedIn = true,
            greeting = $"Hey {GetDisplayName()}, I'm Amaro. How may I help you?",
            options = BuildMenuOptions(menuItems, userId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] AmaroAskRequest request)
    {
        var message = (request.Message ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return Json(new AmaroAskResponse(
                "Ask me about bookings, tickets, wallet, transactions, profile, admin access, or available menus.",
                Array.Empty<AmaroQuickOption>()));
        }

        if (!TryGetCurrentUserId(out var userId))
        {
            await SaveConversation(null, message, "Please login before I show account details.");

            return Json(new AmaroAskResponse(
                "Please login first. After login I can show details based on your role and permissions.",
                new[]
                {
                    new AmaroQuickOption("Login", "/Auth/Login"),
                    new AmaroQuickOption("Signup", "/Auth/Signup")
                }));
        }

        var reply = await BuildReply(userId, message);
        await SaveConversation(userId, message, reply.Message);

        return Json(reply);
    }

    private async Task<AmaroAskResponse> BuildReply(int userId, string message)
    {
        var normalized = message.ToLowerInvariant();
        var menuItems = await GetAccessibleMenus(userId);
        var quickLinks = menuItems
            .Where(x => !string.IsNullOrWhiteSpace(x.RoutePath))
            .Take(6)
            .Select(x => new AmaroQuickOption(x.MenuName ?? x.MenuCode ?? "Open", x.RoutePath!))
            .ToArray();

        if (normalized.Contains("menu") || normalized.Contains("access") || normalized.Contains("permission") || normalized.Contains("role"))
        {
            var roles = await GetRoleNames(userId);
            var modules = await _context.VwUserAccessMatrices
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .Select(x => x.ModuleName ?? x.ModuleCode ?? "Module")
                .Distinct()
                .OrderBy(x => x)
                .Take(10)
                .ToListAsync();

            return new AmaroAskResponse(
                $"Your active role access: {FormatList(roles)}. Available modules: {FormatList(modules)}.",
                quickLinks);
        }

        if (normalized.Contains("booking") || normalized.Contains("ticket") || normalized.Contains("seat"))
        {
            var bookings = await _context.VwBookingCompleteDetails
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.BookedAt)
                .Take(3)
                .Select(x => new
                {
                    x.ShowTitle,
                    x.BookingStatus,
                    x.PaymentStatus,
                    x.StartTime,
                    x.SeatNumbers
                })
                .ToListAsync();

            var summary = bookings.Any()
                ? string.Join(" | ", bookings.Select(x => $"{x.ShowTitle}: {x.BookingStatus}/{x.PaymentStatus}, {x.StartTime:dd MMM hh:mm tt}, seats {NullText(x.SeatNumbers)}"))
                : "No bookings found for your account.";

            return new AmaroAskResponse(
                summary,
                new[] { new AmaroQuickOption("My Bookings", "/Booking/MyBookings") });
        }

        if (normalized.Contains("transaction") || normalized.Contains("payment") || normalized.Contains("refund"))
        {
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .Select(x => new
                {
                    x.TransactionRef,
                    x.Status,
                    x.Amount,
                    x.PaymentMethod
                })
                .ToListAsync();

            var summary = transactions.Any()
                ? string.Join(" | ", transactions.Select(x => $"{x.TransactionRef}: {x.Status}, INR {x.Amount:0.00}, {NullText(x.PaymentMethod)}"))
                : "No transactions found for your account.";

            return new AmaroAskResponse(
                summary,
                new[] { new AmaroQuickOption("Transactions", "/Transaction/History") });
        }

        if (normalized.Contains("wallet"))
        {
            var wallet = await _context.VwWalletSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            var summary = wallet == null
                ? "Wallet details are not available for your account yet."
                : $"Wallet status: {wallet.WalletStatus}. Balance: INR {wallet.WalletBalance:0.00}. Blocked: INR {wallet.BlockedBalance:0.00}.";

            return new AmaroAskResponse(
                summary,
                new[] { new AmaroQuickOption("Wallet", "/Wallet/Index") });
        }

        if (normalized.Contains("show") || normalized.Contains("movie") || normalized.Contains("standup") || normalized.Contains("live"))
        {
            var counts = new[]
            {
                $"Movies {_context.Movies.Count()}",
                $"Standup {_context.StandupShows.Count()}",
                $"Live streams {_context.LiveStreams.Count()}"
            };

            return new AmaroAskResponse(
                $"Current catalog: {string.Join(", ", counts)}. You can browse and book from Home.",
                new[] { new AmaroQuickOption("Browse Shows", "/Home/Index") });
        }

        if ((normalized.Contains("admin") || normalized.Contains("user") || normalized.Contains("version")) &&
            (_rbacService.HasPermission(userId, "ADMIN", "VIEW") || _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_ADMIN", "ADMIN")))
        {
            var userCount = await _context.Users.CountAsync();
            var bookingCount = await _context.VwBookingCompleteDetails.CountAsync();
            var version = await _context.ApplicationVersions
                .AsNoTracking()
                .OrderByDescending(x => x.IsCurrent)
                .ThenByDescending(x => x.UpdatedAt)
                .Select(x => x.VersionNumber)
                .FirstOrDefaultAsync();

            return new AmaroAskResponse(
                $"Admin summary: {userCount} users, {bookingCount} bookings, current version {version ?? "1.0.0"}.",
                new[] { new AmaroQuickOption("Admin Dashboard", "/Admin/Dashboard") });
        }

        return new AmaroAskResponse(
            "I can help with your allowed menus, bookings, tickets, wallet, transactions, shows, profile, and admin summaries when your role permits it.",
            quickLinks);
    }

    private Task<List<AmaroMenuItem>> GetAccessibleMenus(int userId)
    {
        var menus = _rbacService.GetMenus(userId)
            .Where(x => x.CanView)
            .Select(x => new AmaroMenuItem(
                x.MenuCode,
                x.MenuName,
                x.RoutePath))
            .ToList();

        return Task.FromResult(menus);
    }

    private async Task<List<string>> GetRoleNames(int userId)
    {
        return await _context.UserRoleMappings
            .AsNoTracking()
            .Join(
                _context.Roles.AsNoTracking(),
                mapping => mapping.RoleId,
                role => role.Id,
                (mapping, role) => new
                {
                    mapping.UserId,
                    mapping.IsActive,
                    role.RoleName,
                    role.RoleCode,
                    RoleIsActive = role.IsActive
                })
            .Where(x => x.UserId == userId && x.IsActive && x.RoleIsActive)
            .Select(x => x.RoleName ?? x.RoleCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    private async Task SaveConversation(int? userId, string message, string reply)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO public.amaro_chat_messages
(
    user_id,
    user_message,
    amaro_reply,
    request_path,
    created_at
)
VALUES
(
    {userId},
    {message},
    {reply},
    {HttpContext.Request.Headers.Referer.ToString()},
    CURRENT_TIMESTAMP
);
");
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        return int.TryParse(HttpContext.Session.GetString("UserId"), out userId);
    }

    private string GetDisplayName()
    {
        return HttpContext.Session.GetString("UserName")
            ?? HttpContext.Session.GetString("UserEmail")
            ?? "User";
    }

    private static string[] BuildMenuOptions(List<AmaroMenuItem> menuItems, int userId)
    {
        var options = menuItems
            .Select(x => x.MenuName ?? x.MenuCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(5)
            .ToList();

        if (!options.Any())
        {
            options.AddRange(new[] { "My Bookings", "Wallet", "Browse Shows" });
        }

        return options
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToArray();
    }

    private static string FormatList(IEnumerable<string?> items)
    {
        var list = items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return list.Any()
            ? string.Join(", ", list)
            : "none";
    }

    private static string NullText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "NA" : value;
    }

    private record AmaroMenuItem(string? MenuCode, string? MenuName, string? RoutePath);
    public record AmaroAskRequest(string? Message);
    public record AmaroQuickOption(string Label, string Url);
    public record AmaroAskResponse(string Message, AmaroQuickOption[] Options);
}
