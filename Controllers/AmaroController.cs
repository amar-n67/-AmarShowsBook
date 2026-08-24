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
                greeting = "Hey guest, I'm Amaro. I can find today's shows now; login is needed only when you book or view account details.",
                options = new[] { "Today's show times", "Movies", "Standup", "Live streams", "Login" }
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
            var publicReply = await BuildPublicReply(message);
            if (publicReply != null)
            {
                await SaveConversation(null, message, publicReply.Message);
                return Json(publicReply);
            }

            await SaveConversation(null, message, "Login is required for booking, tickets, wallet, transactions, profile, and account details.");

            return Json(new AmaroAskResponse(
                "You can browse shows without login. For booking, tickets, wallet, transactions, or profile details, please login first.",
                new[]
                {
                    new AmaroQuickOption("Login", "/Auth/Login"),
                    new AmaroQuickOption("Signup", "/Auth/Signup"),
                    new AmaroQuickOption("Browse Shows", "/Home/ShowTime")
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
            .Take(5)
            .Select(x => new AmaroQuickOption(x.MenuName ?? x.MenuCode ?? "Open", x.RoutePath!))
            .ToArray();
        var matchedMenus = FindMenus(menuItems, normalized)
            .Take(5)
            .Select(x => new AmaroQuickOption(x.MenuName ?? x.MenuCode ?? "Open", x.RoutePath!))
            .ToArray();

        if (IsHelpIntent(normalized))
        {
            return new AmaroAskResponse(
                "I can book shows, show prices and available seats, list upcoming shows, filter movies/standup/live, open role-allowed pages, check wallet/profile/transactions, and switch theme or cursor.",
                BuildAssistantOptions(menuItems, userId).Take(5).ToArray());
        }

        if (IsThemeIntent(normalized))
        {
            return new AmaroAskResponse(
                "Choose a theme and I will switch it instantly.",
                new[]
                {
                    new AmaroQuickOption("Cinema Theme", "", "theme:cinema"),
                    new AmaroQuickOption("Dark Theme", "", "theme:dark"),
                    new AmaroQuickOption("White Theme", "", "theme:white"),
                    new AmaroQuickOption("System Theme", "", "theme:system")
                });
        }

        if (IsCursorIntent(normalized))
        {
            return new AmaroAskResponse(
                "Choose a cursor style. The same cursor will be used across the application.",
                new[]
                {
                    new AmaroQuickOption("Native Cursor", "", "cursor:native"),
                    new AmaroQuickOption("Liquid Cursor", "", "cursor:liquid"),
                    new AmaroQuickOption("Precision Cursor", "", "cursor:precision"),
                    new AmaroQuickOption("Spotlight Cursor", "", "cursor:spotlight")
                });
        }

        if (normalized.Contains("available") || normalized.Contains("price") || normalized.Contains("prices") || normalized.Contains("seat map"))
        {
            var seatReply = await BuildSeatAndPriceReply(normalized);
            if (seatReply != null)
            {
                return seatReply;
            }
        }

        var accountBookingIntent = normalized.Contains("my booking") ||
            normalized.Contains("my bookings") ||
            normalized.Contains("my ticket") ||
            normalized.Contains("my tickets");

        if (!accountBookingIntent &&
            (IsShowDiscoveryIntent(normalized) || IsBookShowIntent(normalized)))
        {
            var showReply = await BuildShowReply(normalized, requireLoginToBook: false);
            if (showReply != null)
            {
                return showReply;
            }
        }

        if (IsOpenPageIntent(normalized))
        {
            var pageOptions = BuildPageOptions(menuItems, userId, normalized).Take(5).ToArray();
            if (pageOptions.Any())
            {
                return new AmaroAskResponse(
                    $"I can open these allowed pages: {FormatList(pageOptions.Select(x => x.Label))}.",
                    pageOptions);
            }
        }

        if (matchedMenus.Any() && (normalized.Contains("open") || normalized.Contains("go") || normalized.Contains("find") || normalized.Contains("page") || normalized.Contains("menu") || normalized.Contains("where")))
        {
            return new AmaroAskResponse(
                $"I found these allowed app areas: {FormatList(matchedMenus.Select(x => x.Label))}.",
                matchedMenus);
        }

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

        if (normalized.Contains("upcoming") && (normalized.Contains("booked") || normalized.Contains("booking") || normalized.Contains("ticket")))
        {
            var upcomingBookings = await _context.VwBookingCompleteDetails
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.StartTime >= DateTime.UtcNow)
                .OrderBy(x => x.StartTime)
                .Take(5)
                .ToListAsync();

            var summary = upcomingBookings.Any()
                ? string.Join(" | ", upcomingBookings.Select(x => $"{x.BookingRef}: {x.ShowTitle}, {x.StartTime:dd MMM hh:mm tt}, seats {NullText(x.SeatNumbers)}, {x.BookingStatus}/{x.PaymentStatus}"))
                : "You do not have upcoming booked shows right now.";

            return new AmaroAskResponse(
                summary,
                new[] { new AmaroQuickOption("My Bookings", "/Booking/MyBookings") });
        }

        if (normalized.Contains("booking") || normalized.Contains("ticket") || normalized.Contains("seat") || LooksLikeBookingReference(normalized))
        {
            var bookingQuery = _context.VwBookingCompleteDetails
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            var searchTerms = ExtractSearchTerms(normalized);
            var bookingRows = await bookingQuery
                .OrderByDescending(x => x.BookedAt)
                .Take(50)
                .ToListAsync();

            if (searchTerms.Any())
            {
                bookingRows = bookingRows
                    .Where(x => searchTerms.Any(term =>
                        (x.BookingRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (x.ShowTitle ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (x.SeatNumbers ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (x.BookingStatus ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        (x.PaymentStatus ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            var bookings = bookingRows
                .Take(3)
                .Select(x => new
                {
                    x.BookingId,
                    x.BookingRef,
                    x.ShowTitle,
                    x.BookingStatus,
                    x.PaymentStatus,
                    x.StartTime,
                    x.SeatNumbers,
                    x.CancelledAt
                })
                .ToList();

            var summary = bookings.Any()
                ? string.Join(" | ", bookings.Select(x => $"{x.BookingRef}: {x.ShowTitle}, {x.BookingStatus}/{x.PaymentStatus}, {x.StartTime:dd MMM hh:mm tt}, seats {NullText(x.SeatNumbers)}"))
                : "No bookings found for your account.";

            var options = bookings
                .Select(x => new AmaroQuickOption($"Ticket {x.BookingRef}", $"/Booking/TicketByBooking/{x.BookingId}"))
                .Concat(new[] { new AmaroQuickOption("My Bookings", "/Booking/MyBookings") })
                .Take(6)
                .ToArray();

            if (normalized.Contains("cancel"))
            {
                summary += " To cancel, open My Bookings and use Cancel Ticket on a confirmed upcoming booking.";
            }

            return new AmaroAskResponse(
                summary,
                options);
        }

        if (normalized.Contains("transaction") || normalized.Contains("payment") || normalized.Contains("refund"))
        {
            var transactionQuery = _context.Transactions
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Where(x => !x.IsDeleted.HasValue || !x.IsDeleted.Value);

            if (normalized.Contains("success"))
            {
                transactionQuery = transactionQuery.Where(x => x.Status == "SUCCESS");
            }
            else if (normalized.Contains("pending"))
            {
                transactionQuery = transactionQuery.Where(x => x.Status == "PENDING");
            }
            else if (normalized.Contains("failed") || normalized.Contains("fail"))
            {
                transactionQuery = transactionQuery.Where(x => x.Status == "FAILED");
            }

            var transactions = await transactionQuery
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
                new[] { new AmaroQuickOption("Wallet", "/Wallet/MyWallet") });
        }

        if (normalized.Contains("profile") || normalized.Contains("account") || normalized.Contains("email") || normalized.Contains("phone"))
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            var summary = user == null
                ? "I could not find your profile details yet."
                : $"Profile: {user.Name}, {user.Email}, phone {NullText(user.Mobile)}, language {NullText(user.Language)}, genre {NullText(user.Genre)}.";

            return new AmaroAskResponse(
                summary,
                new[] { new AmaroQuickOption("My Profile", "/Profile/MyProfile") });
        }

        if (normalized.Contains("show") || normalized.Contains("movie") || normalized.Contains("standup") || normalized.Contains("live"))
        {
            var showReply = await BuildShowReply(normalized, requireLoginToBook: false);
            if (showReply != null)
            {
                return showReply;
            }
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

        if (normalized.Contains("developer") && matchedMenus.Any(x => (x.Label ?? "").Contains("Developer", StringComparison.OrdinalIgnoreCase)))
        {
            return new AmaroAskResponse(
                "Developer profile tools are available from your menu. Edits still follow developer role permission checks.",
                matchedMenus);
        }

        return new AmaroAskResponse(
            matchedMenus.Any()
                ? $"I found related app areas: {FormatList(matchedMenus.Select(x => x.Label))}."
                : "I can help with your allowed menus, bookings, tickets, wallet, transactions, shows, profile, and admin summaries when your role permits it.",
            matchedMenus.Any() ? matchedMenus : quickLinks);
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

    private async Task<AmaroAskResponse?> BuildPublicReply(string message)
    {
        var normalized = message.ToLowerInvariant();

        if (IsHelpIntent(normalized))
        {
            return new AmaroAskResponse(
                "I can find today's shows, filter movies/standup/live streams, show times and venues, and help start booking. Login is required when you choose seats or view account details.",
                new[]
                {
                    new AmaroQuickOption("Today's Shows", "/Home/ShowTime"),
                    new AmaroQuickOption("Login", "/Auth/Login")
                });
        }

        if (IsThemeIntent(normalized))
        {
            return new AmaroAskResponse(
                "Choose a theme and I will switch it instantly.",
                new[]
                {
                    new AmaroQuickOption("Cinema Theme", "", "theme:cinema"),
                    new AmaroQuickOption("Dark Theme", "", "theme:dark"),
                    new AmaroQuickOption("White Theme", "", "theme:white"),
                    new AmaroQuickOption("System Theme", "", "theme:system")
                });
        }

        if (IsCursorIntent(normalized))
        {
            return new AmaroAskResponse(
                "Choose a cursor style. The same cursor will be used across the application.",
                new[]
                {
                    new AmaroQuickOption("Native Cursor", "", "cursor:native"),
                    new AmaroQuickOption("Liquid Cursor", "", "cursor:liquid"),
                    new AmaroQuickOption("Precision Cursor", "", "cursor:precision"),
                    new AmaroQuickOption("Spotlight Cursor", "", "cursor:spotlight")
                });
        }

        if (IsShowDiscoveryIntent(normalized) || IsBookShowIntent(normalized))
        {
            return await BuildShowReply(normalized, requireLoginToBook: true);
        }

        return null;
    }

    private async Task<AmaroAskResponse?> BuildShowReply(string normalized, bool requireLoginToBook)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(30);

        if (normalized.Contains("today"))
        {
            end = start.AddDays(1);
        }
        else if (normalized.Contains("tomorrow"))
        {
            start = start.AddDays(1);
            end = start.AddDays(1);
        }
        else if (normalized.Contains("weekend"))
        {
            var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)start.DayOfWeek + 7) % 7;
            start = start.AddDays(daysUntilSaturday);
            end = start.AddDays(2);
        }

        var type = normalized.Contains("standup")
            ? "Standup"
            : normalized.Contains("live")
                ? "Live"
                : normalized.Contains("movie")
                    ? "Movie"
                    : "";

        var terms = ExtractSearchTerms(normalized)
            .Where(x => !new[] { "today", "tomorrow", "weekend", "time", "times", "movie", "standup", "live", "stream", "shows", "show", "book" }
                .Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var query = _context.HomeShows
            .AsNoTracking()
            .Where(x => x.ScheduleId > 0 && x.StartTime >= start && x.StartTime < end);

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.ShowType == type);
        }

        var rows = await query
            .OrderBy(x => x.StartTime)
            .Take(80)
            .ToListAsync();

        if (terms.Any())
        {
            rows = rows
                .Where(x => terms.Any(term =>
                    (x.Title ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Location ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.State ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Country ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.VenueName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.ScreenName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var shows = rows.Take(5).ToList();
        if (!shows.Any())
        {
            return new AmaroAskResponse(
                "I could not find matching upcoming shows. Try asking for movies, standup, live streams, today, tomorrow, or a show name.",
                new[] { new AmaroQuickOption("Browse Shows", "/Home/ShowTime") });
        }

        var summary = string.Join(" | ", shows.Select(x =>
        {
            var venue = FormatList(new[] { x.VenueName, x.ScreenName, x.Location });
            return $"{x.Title} - {x.StartTime:dd MMM, hh:mm tt} at {venue}";
        }));

        var wantsBooking = IsBookShowIntent(normalized);
        var message = wantsBooking
            ? $"{summary}. Pick a show time, then choose date/time and seats on the seat map. Login will be requested before booking if needed."
            : summary;

        var options = shows
            .Select(x => new AmaroQuickOption(
                wantsBooking ? $"Book {x.StartTime:hh:mm tt}" : $"{x.Title} {x.StartTime:hh:mm tt}",
                $"/Booking/Seats/{x.ScheduleId}"))
            .Prepend(new AmaroQuickOption("Upcoming Shows", "/Home/ShowTime"))
            .Prepend(new AmaroQuickOption("Browse Shows", string.IsNullOrWhiteSpace(type) ? "/Home/ShowTime" : $"/Home/ShowTime?type={type}"))
            .Prepend(new AmaroQuickOption("Filter Movies", "", "filter-type:Movie"))
            .Prepend(new AmaroQuickOption("Filter Standup", "", "filter-type:Standup"))
            .Prepend(new AmaroQuickOption("Filter Live", "", "filter-type:Live"))
            .Take(5)
            .ToArray();

        if (requireLoginToBook && wantsBooking)
        {
            message += " You can review shows as a guest; seat selection and payment require login.";
        }

        return new AmaroAskResponse(message, options);
    }

    private async Task<AmaroAskResponse?> BuildSeatAndPriceReply(string normalized)
    {
        var showReplyStart = DateTime.UtcNow;
        var rows = await _context.HomeShows
            .AsNoTracking()
            .Where(x => x.ScheduleId > 0 && x.StartTime >= showReplyStart)
            .OrderBy(x => x.StartTime)
            .Take(50)
            .ToListAsync();

        var terms = ExtractSearchTerms(normalized)
            .Where(x => !new[] { "seat", "seats", "available", "price", "prices", "show", "shows", "movie", "standup", "live" }
                .Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (terms.Any())
        {
            rows = rows
                .Where(x => terms.Any(term =>
                    (x.Title ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Location ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.VenueName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.ScreenName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var show = rows.FirstOrDefault();
        if (show == null)
        {
            return null;
        }

        var seats = await _context.ScreenSeats
            .AsNoTracking()
            .Where(x => x.ScheduleId == show.ScheduleId && x.IsActive)
            .Select(x => new { x.Id, x.SeatCategory, x.SeatPrice })
            .ToListAsync();

        var lockedSeatIds = await _context.SeatLocks
            .AsNoTracking()
            .Where(x => x.ScheduleId == show.ScheduleId && (x.LockStatus == "LOCKED" || x.LockStatus == "CONFIRMED"))
            .Select(x => x.ScreenSeatId)
            .Distinct()
            .ToListAsync();

        var available = seats.Where(x => !lockedSeatIds.Contains(x.Id)).ToList();
        var priceBands = seats
            .GroupBy(x => x.SeatCategory)
            .OrderBy(x => x.Min(seat => seat.SeatPrice))
            .Select(x => $"{x.Key} INR {x.Min(seat => seat.SeatPrice):0.00}-{x.Max(seat => seat.SeatPrice):0.00}")
            .ToList();

        var message = $"{show.Title} on {show.StartTime:dd MMM hh:mm tt}: {available.Count} of {seats.Count} seats available. Prices: {FormatList(priceBands)}.";

        return new AmaroAskResponse(
            message,
            new[]
            {
                new AmaroQuickOption("Open Seat Map", $"/Booking/Seats/{show.ScheduleId}"),
                new AmaroQuickOption("Browse Shows", "/Home/ShowTime")
            });
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

        options.AddRange(new[] { "Book a show", "Available seats", "Transactions", "Change theme", "Change cursor" });

        return options
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(x => x!)
            .ToArray();
    }

    private IEnumerable<AmaroQuickOption> BuildAssistantOptions(List<AmaroMenuItem> menuItems, int userId)
    {
        yield return new AmaroQuickOption("Book Shows", "/Home/ShowTime");
        yield return new AmaroQuickOption("Available Seats", "", "show-suggestions");
        yield return new AmaroQuickOption("Upcoming Shows", "/Home/ShowTime");
        yield return new AmaroQuickOption("Movies", "", "filter-type:Movie");
        yield return new AmaroQuickOption("Standup", "", "filter-type:Standup");
        yield return new AmaroQuickOption("Live Streams", "", "filter-type:Live");
        yield return new AmaroQuickOption("My Bookings", "/Booking/MyBookings");
        yield return new AmaroQuickOption("Transactions", "/Transaction/History");
        yield return new AmaroQuickOption("Wallet", "/Wallet/MyWallet");
        yield return new AmaroQuickOption("Profile", "/Profile/MyProfile");
        yield return new AmaroQuickOption("Theme", "", "theme-options");
        yield return new AmaroQuickOption("Cursor", "", "cursor-options");

        foreach (var menu in menuItems.Where(x => !string.IsNullOrWhiteSpace(x.RoutePath)).Take(6))
        {
            yield return new AmaroQuickOption(menu.MenuName ?? menu.MenuCode ?? "Open", menu.RoutePath!);
        }
    }

    private IEnumerable<AmaroQuickOption> BuildPageOptions(List<AmaroMenuItem> menuItems, int userId, string normalized)
    {
        var pageOptions = new List<AmaroQuickOption>
        {
            new("Home", "/Home/ShowTime"),
            new("Booking Page", "/Home/ShowTime"),
            new("My Bookings", "/Booking/MyBookings"),
            new("Transactions", "/Transaction/History"),
            new("Wallet", "/Wallet/MyWallet"),
            new("Profile", "/Profile/MyProfile")
        };

        if (_rbacService.HasPermission(userId, "ADMIN", "VIEW") ||
            _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_ADMIN", "ADMIN"))
        {
            pageOptions.Add(new AmaroQuickOption("Admin Dashboard", "/Admin/Dashboard"));
        }

        if (_rbacService.HasPermission(userId, "DEVELOPER", "EDIT") ||
            _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_DEVELOPER", "DEVELOPER"))
        {
            pageOptions.Add(new AmaroQuickOption("Developer Page", "/Developer/Profile"));
        }

        pageOptions.AddRange(menuItems
            .Where(x => !string.IsNullOrWhiteSpace(x.RoutePath))
            .Select(x => new AmaroQuickOption(x.MenuName ?? x.MenuCode ?? "Open", x.RoutePath!)));

        var terms = ExtractSearchTerms(normalized);
        var matched = pageOptions
            .Where(x => !terms.Any() || terms.Any(term =>
                x.Label.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.Url.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .GroupBy(x => x.Url)
            .Select(x => x.First())
            .ToList();

        return matched.Any() ? matched : pageOptions.GroupBy(x => x.Url).Select(x => x.First());
    }

    private static bool IsHelpIntent(string normalized)
    {
        return normalized is "help" or "hi" or "hello" or "hey" ||
            normalized.Contains("what can you do") ||
            normalized.Contains("how can you help");
    }

    private static bool IsThemeIntent(string normalized)
    {
        return normalized.Contains("theme") ||
            normalized.Contains("dark mode") ||
            normalized.Contains("white mode") ||
            normalized.Contains("cinema mode");
    }

    private static bool IsCursorIntent(string normalized)
    {
        return normalized.Contains("cursor") ||
            normalized.Contains("mouse style") ||
            normalized.Contains("pointer style");
    }

    private static bool IsOpenPageIntent(string normalized)
    {
        return normalized.Contains("open") ||
            normalized.Contains("go to") ||
            normalized.Contains("navigate") ||
            normalized.Contains("page") ||
            normalized.Contains("homepage") ||
            normalized.Contains("home page") ||
            normalized.Contains("wallet page") ||
            normalized.Contains("profile page") ||
            normalized.Contains("developer page") ||
            normalized.Contains("booking page");
    }

    private static bool LooksLikeBookingReference(string normalized)
    {
        return normalized.Contains("bkg") || normalized.Contains("draft") || normalized.Contains("tkt");
    }

    private static bool IsShowDiscoveryIntent(string normalized)
    {
        return normalized.Contains("show") ||
            normalized.Contains("movie") ||
            normalized.Contains("standup") ||
            normalized.Contains("live") ||
            normalized.Contains("today") ||
            normalized.Contains("tomorrow") ||
            normalized.Contains("time") ||
            normalized.Contains("schedule");
    }

    private static bool IsBookShowIntent(string normalized)
    {
        return normalized.Contains("book") ||
            normalized.Contains("seat") ||
            normalized.Contains("reserve");
    }

    private static List<string> ExtractSearchTerms(string normalized)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "find", "show", "shows", "open", "my", "the", "booking", "book", "ticket", "seat", "status", "please", "for", "about", "cancel"
        };

        return normalized
            .Split(new[] { ' ', ',', '.', ':', ';', '/', '\\', '#', '?' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 2 && !stopWords.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static IEnumerable<AmaroMenuItem> FindMenus(IEnumerable<AmaroMenuItem> menuItems, string normalized)
    {
        var terms = ExtractSearchTerms(normalized);
        if (!terms.Any())
        {
            terms = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length > 2)
                .Take(4)
                .ToList();
        }

        return menuItems
            .Where(x => !string.IsNullOrWhiteSpace(x.RoutePath))
            .Select(x => new
            {
                Menu = x,
                Text = $"{x.MenuCode} {x.MenuName} {x.RoutePath}".ToLowerInvariant()
            })
            .Where(x => terms.Any(term => x.Text.Contains(term)))
            .OrderBy(x => x.Menu.MenuName)
            .Select(x => x.Menu);
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
    public record AmaroQuickOption(string Label, string Url, string? Command = null);
    public record AmaroAskResponse(string Message, AmaroQuickOption[] Options);
}
