using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers;

// The assistant answers from app data first, then limits every admin shortcut to the user's current roles.
public class AmaroController : Controller
{
    private const string SupportPhone = "+91 9651698863";
    private const string SupportEmail = "support@showtime.com";

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
                options = new[]
                {
                    new AmaroQuickOption("Today's Shows", "/Home/ShowTime"),
                    new AmaroQuickOption("Movies", "", "filter-type:Movie"),
                    new AmaroQuickOption("Standup", "", "filter-type:Standup"),
                    new AmaroQuickOption("Live Streams", "", "filter-type:Live"),
                    new AmaroQuickOption("Help", "", "support-options"),
                    new AmaroQuickOption("Login", "/Auth/Login")
                }
            });
        }

        var menuItems = await GetAccessibleMenus(userId);
        var proactiveOptions = await BuildProactiveMenuOptions(userId, menuItems);
        var pageName = GetCurrentPageName();

        return Json(new
        {
            isLoggedIn = true,
            greeting = string.IsNullOrWhiteSpace(pageName)
                ? $"Hey {GetDisplayName()}, I'm Amaro. How may I help you?"
                : $"Hey {GetDisplayName()}, I'm Amaro. I can help with {pageName} and your role-allowed actions.",
            options = proactiveOptions.Any() ? proactiveOptions : BuildMenuOptions(menuItems, userId)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] AmaroAskRequest request)
    {
        // Guests can get show and support help; account, booking, wallet, and admin answers require login.
        var message = (request.Message ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return Json(new AmaroAskResponse(
                "Ask me about bookings, tickets, wallet, transactions, profile, admin access, support, or available menus.",
                BuildSupportOptions()));
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
        // Direct intents are answered before broad menu suggestions, so short commands stay predictable.
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
            var canPrint = _rbacService.CanUsePrintTools(userId);
            return new AmaroAskResponse(
                canPrint
                    ? "I'm Amaro. I can book shows, show prices and available seats, list upcoming shows, filter movies/standup/live, open role-allowed pages, search the current page, print, go back, export allowed admin data, check wallet/profile/transactions, switch theme or cursor, and connect you to support."
                    : "I'm Amaro. I can book shows, show prices and available seats, list upcoming shows, filter movies/standup/live, open role-allowed pages, search the current page, go back, check wallet/profile/transactions, switch theme or cursor, and connect you to support. Print and capture tools are admin-only.",
                BuildAssistantOptions(menuItems, userId).Take(8).ToArray());
        }

        if (IsSupportIntent(normalized))
        {
            return BuildSupportReply();
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

        var pageSearchTerm = ExtractPageSearchTerm(message);
        if (pageSearchTerm != null)
        {
            return new AmaroAskResponse(
                string.IsNullOrWhiteSpace(pageSearchTerm)
                    ? "I can focus the search box on this page. Type what you want to filter, or tell me: search this page for booking, failed, wallet, or any visible text."
                    : $"I will search this page for \"{pageSearchTerm}\".",
                new[]
                {
                    new AmaroQuickOption("Search This Page", "", $"page-search:{pageSearchTerm}"),
                    new AmaroQuickOption("Clear Search", "", "clear-page-search")
                });
        }

        if (IsPrintIntent(normalized))
        {
            if (!_rbacService.CanUsePrintTools(userId))
            {
                return new AmaroAskResponse(
                    "Print is allowed only for Admin and Super Admin.",
                    Array.Empty<AmaroQuickOption>());
            }

            return new AmaroAskResponse(
                "I can print the current page with the app print layout.",
                new[] { new AmaroQuickOption("Print Page", "", "print-page") });
        }

        if (IsBackIntent(normalized))
        {
            return new AmaroAskResponse(
                "I can take you back to the previous page.",
                new[] { new AmaroQuickOption("Go Back", "", "go-back") });
        }

        if (IsProactiveIntent(normalized))
        {
            var proactiveReply = await BuildProactiveReply(userId, normalized, menuItems);
            if (proactiveReply != null)
            {
                return proactiveReply;
            }
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

        var adminReply = await BuildRoleAwareOperationsReply(userId, normalized, menuItems);
        if (adminReply != null)
        {
            return adminReply;
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
            (_rbacService.HasPermission(userId, "ADMIN", "VIEW") || _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_ADMIN")))
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
                : "I can help with your allowed menus, bookings, tickets, wallet, transactions, shows, profile, support, and admin summaries when your role permits it.",
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
                "I'm Amaro. I can find today's shows, filter movies/standup/live streams, show times and venues, help start booking, search this page, go back, and connect you to support. Print and capture tools are admin-only.",
                new[]
                {
                    new AmaroQuickOption("Today's Shows", "/Home/ShowTime"),
                    new AmaroQuickOption("Go Back", "", "go-back"),
                    new AmaroQuickOption("Help", "", "support-options"),
                    new AmaroQuickOption("Login", "/Auth/Login")
                });
        }

        if (IsSupportIntent(normalized))
        {
            return BuildSupportReply();
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

        var pageSearchTerm = ExtractPageSearchTerm(message);
        if (pageSearchTerm != null)
        {
            return new AmaroAskResponse(
                string.IsNullOrWhiteSpace(pageSearchTerm)
                    ? "I can focus the search box on this page. Type what you want to filter, or tell me: search this page for movie, standup, live, or any visible text."
                    : $"I will search this page for \"{pageSearchTerm}\".",
                new[]
                {
                    new AmaroQuickOption("Search This Page", "", $"page-search:{pageSearchTerm}"),
                    new AmaroQuickOption("Clear Search", "", "clear-page-search")
                });
        }

        if (IsPrintIntent(normalized))
        {
            return new AmaroAskResponse(
                "Print is allowed only for Admin and Super Admin.",
                Array.Empty<AmaroQuickOption>());
        }

        if (IsBackIntent(normalized))
        {
            return new AmaroAskResponse(
                "I can take you back to the previous page.",
                new[] { new AmaroQuickOption("Go Back", "", "go-back") });
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

    private async Task<AmaroAskResponse?> BuildRoleAwareOperationsReply(
        int userId,
        string normalized,
        List<AmaroMenuItem> menuItems)
    {
        // Admin and developer answers are only built after RBAC confirms the matching module.
        var wantsAdmin =
            normalized.Contains("admin") ||
            normalized.Contains("dashboard") ||
            normalized.Contains("manage") ||
            normalized.Contains("report") ||
            normalized.Contains("summary") ||
            normalized.Contains("export") ||
            normalized.Contains("refund") ||
            normalized.Contains("wallet") ||
            normalized.Contains("notification") ||
            normalized.Contains("security") ||
            normalized.Contains("scanner") ||
            normalized.Contains("coupon") ||
            normalized.Contains("activity") ||
            normalized.Contains("version") ||
            normalized.Contains("role") ||
            normalized.Contains("permission") ||
            normalized.Contains("user");

        var wantsDeveloper =
            normalized.Contains("developer") ||
            normalized.Contains("profile editor") ||
            normalized.Contains("portfolio");

        if (!wantsAdmin && !wantsDeveloper)
        {
            return null;
        }

        var allowedModules = await _context.VwUserAccessMatrices
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => new { x.ModuleCode, x.ModuleName, x.ActionType })
            .Distinct()
            .ToListAsync();

        var allowedModuleCodes = allowedModules
            .Select(x => x.ModuleCode ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool Can(string module, string action = "VIEW")
        {
            return _rbacService.HasPermission(userId, module, action) ||
                allowedModules.Any(x =>
                    string.Equals(x.ModuleCode, module, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.ActionType, action, StringComparison.OrdinalIgnoreCase));
        }

        var canExport = _rbacService.IsSuperAdmin(userId);

        if (wantsDeveloper && !Can("DEVELOPER", "EDIT") && !_rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_DEVELOPER"))
        {
            return new AmaroAskResponse(
                "Your current role does not include developer tools. I can still help with your allowed pages, bookings, shows, wallet, profile, and transactions.",
                BuildPageOptions(menuItems, userId, normalized).Take(5).ToArray());
        }

        if (wantsAdmin && !allowedModuleCodes.Any(code => code is "ADMIN" or "USER" or "ROLE" or "SHOW" or "BOOKING" or "PAYMENT" or "REFUND" or "WALLET" or "COUPON" or "NOTIFICATION" or "SCANNER"))
        {
            return new AmaroAskResponse(
                "Your current role does not include admin operations. I will only show account and booking actions that your role allows.",
                BuildPageOptions(menuItems, userId, normalized).Take(5).ToArray());
        }

        if (normalized.Contains("export"))
        {
            if (!canExport)
            {
                return new AmaroAskResponse(
                    "Only Super Admin can export data.",
                    BuildPageOptions(menuItems, userId, normalized)
                        .Where(x => x.Url.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase))
                        .Take(4)
                        .ToArray());
            }

            if (normalized.Contains("dashboard"))
            {
                return new AmaroAskResponse(
                    "I can generate the full dashboard Excel workbook with logo, sheets, charts, and auto-width columns.",
                    new[]
                    {
                        new AmaroQuickOption("Export Dashboard", "", "export-dashboard"),
                        new AmaroQuickOption("Admin Dashboard", "/Admin/Dashboard")
                    });
            }

            var exportOptions = BuildPageOptions(menuItems, userId, normalized)
                .Where(x => x.Url.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .Append(new AmaroQuickOption("Export This Page", "", "admin-export"))
                .Append(new AmaroQuickOption("Export Dashboard", "", "export-dashboard"))
                .ToArray();

            return new AmaroAskResponse(
                "Export follows page access. Open any allowed admin table, apply filters if needed, then use Export This Page.",
                exportOptions);
        }

        if (normalized.Contains("search") || normalized.Contains("find") || normalized.Contains("lookup"))
        {
            var searchReply = await BuildAdminSearchReply(userId, normalized, Can);
            if (searchReply != null)
            {
                return searchReply;
            }
        }

        if (IsAdminOverviewIntent(normalized))
        {
            return await BuildAdminOverviewReply(userId, menuItems, Can);
        }

        if (normalized.Contains("refund") && Can("REFUND"))
        {
            var pending = await _context.VwRefundSummaries.CountAsync(x => x.RefundStatus == "PENDING");
            var failed = await _context.VwRefundSummaries.CountAsync(x => x.RefundStatus == "FAILED" || x.IsRefundError == 1);

            return new AmaroAskResponse(
                $"Refund desk: {pending} pending, {failed} failed/error. Approve, reject, retry, and notes still run through the refund detail page for audit safety.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Refunds", "/Admin/Refunds"),
                    new AmaroQuickOption("Admin Dashboard", "/Admin/Dashboard"),
                    new AmaroQuickOption("Export Refunds", "/Admin/ExportRefunds")
                }));
        }

        if ((normalized.Contains("security") || normalized.Contains("scanner") || normalized.Contains("ticket scan")) && Can("SCANNER"))
        {
            var issues = await _context.VwTicketValidationSummaries.CountAsync(x => x.IsSecurityIssue == 1);
            var invalid = await _context.VwTicketValidationSummaries.CountAsync(x => x.ValidationStatus == "INVALID" || x.ValidationStatus == "DUPLICATE");
            var devices = await _context.ScannerDevices.CountAsync();

            return new AmaroAskResponse(
                $"Security desk: {issues} open issue rows, {invalid} invalid/duplicate scans, {devices} scanner devices registered.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Security", "/Admin/Security"),
                    new AmaroQuickOption("Scanner Devices", "/Admin/Security", "ask:open security scanner devices"),
                    new AmaroQuickOption("Export Security", "", "admin-export")
                }));
        }

        if ((normalized.Contains("wallet") || normalized.Contains("balance")) && Can("WALLET"))
        {
            var active = await _context.VwWalletSummaries.CountAsync(x => x.WalletStatus == "ACTIVE");
            var blocked = await _context.VwWalletSummaries.CountAsync(x => x.WalletStatus == "BLOCKED" || x.WalletStatus == "SUSPENDED");
            var totalBalance = await _context.VwWalletSummaries.SumAsync(x => x.WalletBalance);

            return new AmaroAskResponse(
                $"Wallet desk: {active} active wallets, {blocked} blocked/suspended wallets, total balance INR {totalBalance:0.00}.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Wallets", "/Admin/Wallets"),
                    new AmaroQuickOption("Export Wallets", "", "admin-export")
                }));
        }

        if ((normalized.Contains("notification") || normalized.Contains("message")) && Can("NOTIFICATION"))
        {
            var pending = await _context.VwNotificationCenters.CountAsync(x => x.Status == "PENDING" || x.Status == "PROCESSING");
            var errors = await _context.VwNotificationCenters.CountAsync(x => x.IsError == 1 || x.Status == "FAILED" || x.Status == "ERROR");

            return new AmaroAskResponse(
                $"Notification center: {pending} pending/processing, {errors} failed/error.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Notifications", "/Admin/Notifications"),
                    new AmaroQuickOption("Export Notifications", "", "admin-export")
                }));
        }

        if ((normalized.Contains("coupon") || normalized.Contains("discount")) && Can("COUPON"))
        {
            var used = await _context.VwCouponUsages.CountAsync();
            var discount = await _context.VwCouponUsages.SumAsync(x => x.DiscountAmount ?? 0);

            return new AmaroAskResponse(
                $"Coupon usage: {used} usage rows, total discount INR {discount:0.00}.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Coupon Used", "/Admin/CouponUsage"),
                    new AmaroQuickOption("Export Coupons", "", "admin-export")
                }));
        }

        if ((normalized.Contains("transaction") || normalized.Contains("payment")) && Can("PAYMENT"))
        {
            var success = await _context.VwBookingTransactionSummaries.CountAsync(x => x.TransactionStatus == "SUCCESS");
            var failed = await _context.VwBookingTransactionSummaries.CountAsync(x => x.TransactionStatus == "FAILED" || x.IsPaymentError == 1);

            return new AmaroAskResponse(
                $"Payment desk: {success} successful transactions, {failed} failed/error transactions.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Admin Transactions", "/Admin/Transactions"),
                    new AmaroQuickOption("Export Transactions", "", "admin-export")
                }));
        }

        if ((normalized.Contains("booking") || normalized.Contains("ticket")) && Can("BOOKING"))
        {
            var confirmed = await _context.VwBookingCompleteDetails.CountAsync(x => x.BookingStatus == "CONFIRMED");
            var cancelled = await _context.VwBookingCompleteDetails.CountAsync(x => x.BookingStatus == "CANCELLED");

            return new AmaroAskResponse(
                $"Booking desk: {confirmed} confirmed bookings, {cancelled} cancelled bookings.",
                FilterExportOptions(canExport,
                new[]
                {
                    new AmaroQuickOption("Admin Bookings", "/Admin/Bookings"),
                    new AmaroQuickOption("Export Bookings", "", "admin-export")
                }));
        }

        if ((normalized.Contains("role") || normalized.Contains("permission") || normalized.Contains("access")) && (Can("ROLE") || Can("PERMISSION") || Can("USER", "GRANT_ACCESS")))
        {
            var roles = await GetRoleNames(userId);
            var modules = allowedModules
                .Select(x => x.ModuleName ?? x.ModuleCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .Take(12)
                .ToList();

            return new AmaroAskResponse(
                $"Your roles: {FormatList(roles)}. Allowed operation areas: {FormatList(modules)}. I will not show actions outside this access.",
                new[]
                {
                    new AmaroQuickOption("Roles", "/Admin/Roles"),
                    new AmaroQuickOption("User Access", "/Admin/UserAccess"),
                    new AmaroQuickOption("Users", "/Admin/Users")
                }.Where(x => menuItems.Any(m => string.Equals(m.RoutePath, x.Url, StringComparison.OrdinalIgnoreCase))).ToArray());
        }

        if (wantsDeveloper && (Can("DEVELOPER", "EDIT") || _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_DEVELOPER")))
        {
            return new AmaroAskResponse(
                "Developer tools are available for your role. You can update the developer profile and inspect app implementation notes from the developer page.",
                new[]
                {
                    new AmaroQuickOption("Developer Profile", "/Developer/Profile"),
                    new AmaroQuickOption("Developer Overview", "/Developer/Index")
                });
        }

        var adminLinks = BuildPageOptions(menuItems, userId, normalized)
            .Where(x => x.Url.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase) || x.Url.StartsWith("/Developer/", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToArray();

        if (adminLinks.Any())
        {
            return new AmaroAskResponse(
                $"I can help with these role-allowed operation pages: {FormatList(adminLinks.Select(x => x.Label))}. Ask for a summary, search, export, refund, wallet, security, users, roles, shows, or notifications.",
                adminLinks);
        }

        return null;
    }

    private async Task<AmaroAskResponse?> BuildAdminSearchReply(
        int userId,
        string normalized,
        Func<string, string, bool> can)
    {
        var terms = ExtractSearchTerms(normalized)
            .Where(x => !new[] { "search", "find", "lookup", "admin", "user", "booking", "transaction", "refund", "wallet", "notification", "security" }
                .Contains(x, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (!terms.Any())
        {
            return new AmaroAskResponse(
                "Tell me what to search, for example: find user amar, search booking BKG, lookup refund RF, or find failed payment.",
                Array.Empty<AmaroQuickOption>());
        }

        var results = new List<string>();
        var options = new List<AmaroQuickOption>();

        if (can("USER", "VIEW"))
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Take(200)
                .Select(x => new { x.Id, x.Name, x.Email, x.Mobile })
                .ToListAsync();

            users = users
                .Where(x => terms.Any(term =>
                    (x.Name ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Email ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.Mobile ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .ToList();

            results.AddRange(users.Select(x => $"User {x.Id}: {NullText(x.Name)} ({NullText(x.Email)})"));
            options.AddRange(users.Select(x => new AmaroQuickOption($"User {x.Id}", $"/Admin/UserDetails/{x.Id}")));
        }

        if (can("BOOKING", "VIEW"))
        {
            var bookings = await _context.VwBookingCompleteDetails
                .AsNoTracking()
                .OrderByDescending(x => x.BookedAt)
                .Take(200)
                .Select(x => new { x.BookingId, x.BookingRef, x.ShowTitle, x.BookingStatus, x.UserName, x.UserEmail })
                .ToListAsync();

            bookings = bookings
                .Where(x => terms.Any(term =>
                    (x.BookingRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.ShowTitle ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserEmail ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .ToList();

            results.AddRange(bookings.Select(x => $"{x.BookingRef}: {x.ShowTitle}, {x.BookingStatus}"));
            options.AddRange(bookings.Select(x => new AmaroQuickOption($"Booking {x.BookingRef}", $"/Admin/Bookings")));
        }

        if (can("REFUND", "VIEW"))
        {
            var refunds = await _context.VwRefundSummaries
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(200)
                .Select(x => new { x.RefundId, x.RefundRef, x.BookingRef, x.TransactionRef, x.UserName, x.UserEmail, x.RefundStatus, x.RefundAmount })
                .ToListAsync();

            refunds = refunds
                .Where(x => terms.Any(term =>
                    (x.RefundRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.BookingRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.TransactionRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserEmail ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .ToList();

            results.AddRange(refunds.Select(x => $"{x.RefundRef}: {x.RefundStatus}, INR {(x.RefundAmount ?? 0):0.00}"));
            options.AddRange(refunds.Select(x => new AmaroQuickOption($"Refund {x.RefundRef}", $"/Admin/RefundDetails/{x.RefundId}")));
        }

        if (can("PAYMENT", "VIEW"))
        {
            var payments = await _context.VwBookingTransactionSummaries
                .AsNoTracking()
                .OrderByDescending(x => x.BookingCreatedAt)
                .Take(200)
                .Select(x => new { x.TransactionId, x.TransactionRef, x.BookingRef, x.UserName, x.UserEmail, x.TransactionStatus, x.TransactionAmount })
                .ToListAsync();

            payments = payments
                .Where(x => terms.Any(term =>
                    (x.TransactionRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.BookingRef ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (x.UserEmail ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .ToList();

            results.AddRange(payments.Select(x => $"{x.TransactionRef}: {x.TransactionStatus}, INR {(x.TransactionAmount ?? 0):0.00}"));
            options.AddRange(payments
                .Where(x => x.TransactionId.HasValue)
                .Select(x => new AmaroQuickOption($"Txn {x.TransactionRef}", $"/Admin/TransactionDetails/{x.TransactionId}")));
        }

        return results.Any()
            ? new AmaroAskResponse($"Found: {string.Join(" | ", results.Take(6))}", options.Take(5).ToArray())
            : new AmaroAskResponse(
                "No matching role-allowed records found. I did not search areas your role cannot view.",
                Array.Empty<AmaroQuickOption>());
    }

    private async Task<AmaroQuickOption[]> BuildProactiveMenuOptions(int userId, List<AmaroMenuItem> menuItems)
    {
        var path = GetCurrentPath();
        var options = new List<AmaroQuickOption>();
        var canExport = _rbacService.IsSuperAdmin(userId);

        if (path.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase))
        {
            options.Add(new AmaroQuickOption("This Page Summary", "", "ask:what should i do on this page"));
            options.Add(new AmaroQuickOption("Search Records", "", "ask:search admin records"));
            if (canExport)
            {
                options.Add(new AmaroQuickOption("Export This Page", "", "admin-export"));
            }
            options.Add(new AmaroQuickOption("Admin Overview", "", "ask:admin overview"));
        }
        else if (path.StartsWith("/Booking/Seats", StringComparison.OrdinalIgnoreCase))
        {
            options.Add(new AmaroQuickOption("Seat Prices", "", "ask:available seats and prices"));
            options.Add(new AmaroQuickOption("My Bookings", "/Booking/MyBookings"));
            options.Add(new AmaroQuickOption("Wallet", "/Wallet/MyWallet"));
        }
        else if (path.StartsWith("/Booking/", StringComparison.OrdinalIgnoreCase))
        {
            options.Add(new AmaroQuickOption("Upcoming Bookings", "", "ask:my upcoming bookings"));
            options.Add(new AmaroQuickOption("Transactions", "/Transaction/History"));
            options.Add(new AmaroQuickOption("Browse Shows", "/Home/ShowTime"));
        }
        else if (path.StartsWith("/Transaction/", StringComparison.OrdinalIgnoreCase))
        {
            options.Add(new AmaroQuickOption("Payment Status", "", "ask:my payments summary"));
            options.Add(new AmaroQuickOption("My Bookings", "/Booking/MyBookings"));
            options.Add(new AmaroQuickOption("Wallet", "/Wallet/MyWallet"));
        }
        else if (path.StartsWith("/Wallet/", StringComparison.OrdinalIgnoreCase))
        {
            options.Add(new AmaroQuickOption("Wallet Summary", "", "ask:wallet summary"));
            options.Add(new AmaroQuickOption("Transactions", "/Transaction/History"));
            options.Add(new AmaroQuickOption("Book Shows", "/Home/ShowTime"));
        }
        else
        {
            options.Add(new AmaroQuickOption("Recommended Shows", "", "ask:recommend shows"));
            options.Add(new AmaroQuickOption("Movies", "", "filter-type:Movie"));
            options.Add(new AmaroQuickOption("Available Seats", "", "show-suggestions"));
            options.Add(new AmaroQuickOption("Help", "", "support-options"));
        }

        options.Add(new AmaroQuickOption("Search This Page", "", "page-search:"));
        if (_rbacService.CanUsePrintTools(userId))
        {
            options.Add(new AmaroQuickOption("Print Page", "", "print-page"));
        }
        options.Add(new AmaroQuickOption("Go Back", "", "go-back"));

        var roleOptions = BuildMenuOptions(menuItems, userId)
            .Where(x => options.All(existing =>
                !string.Equals(existing.Label, x.Label, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(existing.Url, x.Url, StringComparison.OrdinalIgnoreCase)))
            .Take(4);

        options.AddRange(roleOptions);
        return await Task.FromResult(options.Take(8).ToArray());
    }

    private async Task<AmaroAskResponse?> BuildProactiveReply(
        int userId,
        string normalized,
        List<AmaroMenuItem> menuItems)
    {
        var path = GetCurrentPath();

        if (path.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("admin") ||
            normalized.Contains("alert") ||
            normalized.Contains("risk"))
        {
            var allowedModules = await _context.VwUserAccessMatrices
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsActive)
                .Select(x => new { x.ModuleCode, x.ActionType })
                .Distinct()
                .ToListAsync();

            bool Can(string module, string action = "VIEW")
            {
                return _rbacService.HasPermission(userId, module, action) ||
                    allowedModules.Any(x =>
                        string.Equals(x.ModuleCode, module, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.ActionType, action, StringComparison.OrdinalIgnoreCase));
            }

            if (Can("ADMIN") || Can("REFUND") || Can("PAYMENT") || Can("SCANNER") || Can("BOOKING"))
            {
                return await BuildAdminOverviewReply(userId, menuItems, Can);
            }
        }

        if (path.StartsWith("/Booking/Seats", StringComparison.OrdinalIgnoreCase))
        {
            var seatReply = await BuildSeatAndPriceReply(normalized);
            return seatReply ?? new AmaroAskResponse(
                "On this page, the strongest next step is to compare available seat categories, pick adjacent seats, then continue to payment. I can also open wallet or your bookings.",
                new[]
                {
                    new AmaroQuickOption("Browse Shows", "/Home/ShowTime"),
                    new AmaroQuickOption("My Bookings", "/Booking/MyBookings"),
                    new AmaroQuickOption("Wallet", "/Wallet/MyWallet")
                });
        }

        if (path.StartsWith("/Transaction/", StringComparison.OrdinalIgnoreCase))
        {
            return new AmaroAskResponse(
                "For transactions, I can quickly separate success, pending, failed, and refund-related payments for your account.",
                new[]
                {
                    new AmaroQuickOption("Successful Payments", "", "ask:successful payments"),
                    new AmaroQuickOption("Failed Payments", "", "ask:failed payments"),
                    new AmaroQuickOption("My Bookings", "/Booking/MyBookings")
                });
        }

        if (path.StartsWith("/Wallet/", StringComparison.OrdinalIgnoreCase))
        {
            return new AmaroAskResponse(
                "For wallet work, I can show balance, blocked balance, and connect wallet activity back to bookings and transactions.",
                new[]
                {
                    new AmaroQuickOption("Wallet Summary", "", "ask:wallet summary"),
                    new AmaroQuickOption("Transactions", "/Transaction/History"),
                    new AmaroQuickOption("Book Shows", "/Home/ShowTime")
                });
        }

        return new AmaroAskResponse(
            "Smart next steps: browse shows, check seats and prices, review your bookings, contact support, or open a role-allowed page. I will keep actions inside your access.",
            BuildAssistantOptions(menuItems, userId).Take(6).ToArray());
    }

    private async Task<AmaroAskResponse> BuildAdminOverviewReply(
        int userId,
        List<AmaroMenuItem> menuItems,
        Func<string, string, bool> can)
    {
        var alerts = new List<string>();
        var options = new List<AmaroQuickOption>();
        var canExport = _rbacService.IsSuperAdmin(userId);

        if (can("REFUND", "VIEW"))
        {
            var pendingRefunds = await _context.VwRefundSummaries.CountAsync(x => x.RefundStatus == "PENDING");
            var failedRefunds = await _context.VwRefundSummaries.CountAsync(x => x.RefundStatus == "FAILED" || x.IsRefundError == 1);
            alerts.Add($"refunds {pendingRefunds} pending, {failedRefunds} error");
            options.Add(new AmaroQuickOption("Refunds", "/Admin/Refunds"));
        }

        if (can("PAYMENT", "VIEW"))
        {
            var failedPayments = await _context.VwBookingTransactionSummaries.CountAsync(x => x.TransactionStatus == "FAILED" || x.IsPaymentError == 1);
            var pendingPayments = await _context.VwBookingTransactionSummaries.CountAsync(x => x.TransactionStatus == "PENDING");
            alerts.Add($"payments {failedPayments} failed, {pendingPayments} pending");
            options.Add(new AmaroQuickOption("Transactions", "/Admin/Transactions"));
        }

        if (can("SCANNER", "VIEW"))
        {
            var securityIssues = await _context.VwTicketValidationSummaries.CountAsync(x => x.IsSecurityIssue == 1);
            var invalidScans = await _context.VwTicketValidationSummaries.CountAsync(x => x.ValidationStatus == "INVALID" || x.ValidationStatus == "DUPLICATE");
            alerts.Add($"scanner {securityIssues} security issues, {invalidScans} invalid scans");
            options.Add(new AmaroQuickOption("Security", "/Admin/Security"));
        }

        if (can("BOOKING", "VIEW"))
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var todayBookings = await _context.VwBookingCompleteDetails.CountAsync(x => x.BookedAt >= today && x.BookedAt < tomorrow);
            alerts.Add($"bookings {todayBookings} created today");
            options.Add(new AmaroQuickOption("Bookings", "/Admin/Bookings"));
        }

        if (can("NOTIFICATION", "VIEW"))
        {
            var notificationErrors = await _context.VwNotificationCenters.CountAsync(x => x.IsError == 1 || x.Status == "FAILED" || x.Status == "ERROR");
            alerts.Add($"notifications {notificationErrors} errors");
            options.Add(new AmaroQuickOption("Notifications", "/Admin/Notifications"));
        }

        options.Add(new AmaroQuickOption("Search Records", "", "ask:search admin records"));
        if (canExport)
        {
            options.Add(new AmaroQuickOption("Export This Page", "", "admin-export"));
            options.Add(new AmaroQuickOption("Export Dashboard", "", "export-dashboard"));
        }
        options.AddRange(BuildPageOptions(menuItems, userId, "admin dashboard")
            .Where(x => x.Url.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase)));

        var message = alerts.Any()
            ? $"Role-aware operations overview: {string.Join(" | ", alerts)}. Start with the highest pending/error area, then export filtered rows if you need a report."
            : "Your role has admin access, but I do not see urgent operational alerts in the modules I can inspect.";

        return new AmaroAskResponse(
            message,
            options
                .Where(x => !string.IsNullOrWhiteSpace(x.Label))
                .GroupBy(x => $"{x.Label}|{x.Url}|{x.Command}", StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .Take(6)
                .ToArray());
    }

    private AmaroAskResponse BuildSupportReply()
    {
        return new AmaroAskResponse(
            "Choose Call Us, Write Us, or WhatsApp Us for support. For booking/payment/refund problems, include your booking reference or transaction reference if you have it.",
            BuildSupportOptions());
    }

    private AmaroQuickOption[] BuildSupportOptions()
    {
        return new[]
        {
            new AmaroQuickOption("Call Us", $"tel:{SupportPhone.Replace(" ", "")}"),
            new AmaroQuickOption("Write Us", $"mailto:{SupportEmail}"),
            new AmaroQuickOption("WhatsApp Us", BuildWhatsAppUrl()),
            new AmaroQuickOption("My Bookings", "/Booking/MyBookings"),
            new AmaroQuickOption("Transactions", "/Transaction/History")
        };
    }

    private string BuildWhatsAppUrl()
    {
        var phone = SupportPhone.Replace("+", "").Replace(" ", "");
        var text = Uri.EscapeDataString($"Hi showTime Team, I'm {GetDisplayName()}. I need support. Please help me with my request.");
        return $"https://wa.me/{phone}?text={text}";
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

    private AmaroQuickOption[] BuildMenuOptions(List<AmaroMenuItem> menuItems, int userId)
    {
        var options = menuItems
            .Where(x => !string.IsNullOrWhiteSpace(x.RoutePath))
            .Select(x => new AmaroQuickOption(x.MenuName ?? x.MenuCode ?? "Open", x.RoutePath!))
            .Take(5)
            .ToList();

        if (!options.Any())
        {
            options.AddRange(new[]
            {
                new AmaroQuickOption("My Bookings", "/Booking/MyBookings"),
                new AmaroQuickOption("Wallet", "/Wallet/MyWallet"),
                new AmaroQuickOption("Browse Shows", "/Home/ShowTime")
                });
        }

        options.AddRange(new[]
        {
            new AmaroQuickOption("Book a Show", "/Home/ShowTime"),
            new AmaroQuickOption("Available Seats", "", "show-suggestions"),
            new AmaroQuickOption("Transactions", "/Transaction/History"),
            new AmaroQuickOption("Help", "", "support-options"),
            new AmaroQuickOption("Change Theme", "", "theme-options"),
            new AmaroQuickOption("Change Cursor", "", "cursor-options")
        });

        if (_rbacService.HasPermission(userId, "ADMIN", "VIEW") ||
            _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_ADMIN"))
        {
            options.AddRange(new[]
            {
                new AmaroQuickOption("Admin Summary", "", "ask:admin summary"),
                new AmaroQuickOption("Search Admin Records", "", "ask:search admin records")
            });

            if (_rbacService.IsSuperAdmin(userId))
            {
                options.Add(new AmaroQuickOption("Export This Page", "", "admin-export"));
                options.Add(new AmaroQuickOption("Export Dashboard", "", "export-dashboard"));
            }
        }

        return options
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .GroupBy(x => $"{x.Label}|{x.Url}|{x.Command}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(8)
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
        yield return new AmaroQuickOption("Help", "", "support-options");
        yield return new AmaroQuickOption("Search This Page", "", "page-search:");
        if (_rbacService.CanUsePrintTools(userId))
        {
            yield return new AmaroQuickOption("Print Page", "", "print-page");
        }
        yield return new AmaroQuickOption("Go Back", "", "go-back");
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
            _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_ADMIN"))
        {
            pageOptions.Add(new AmaroQuickOption("Admin Dashboard", "/Admin/Dashboard"));
        }

        if (_rbacService.HasPermission(userId, "DEVELOPER", "EDIT") ||
            _rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_DEVELOPER"))
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

    private static bool IsPrintIntent(string normalized)
    {
        return normalized is "print" or "print page" or "print this page" ||
            normalized.Contains("print current page") ||
            normalized.Contains("make pdf") ||
            normalized.Contains("download pdf");
    }

    private static bool IsBackIntent(string normalized)
    {
        return normalized is "back" or "go back" ||
            normalized.Contains("previous page") ||
            normalized.Contains("last page");
    }

    private static string? ExtractPageSearchTerm(string message)
    {
        var clean = (message ?? string.Empty).Trim();
        var lower = clean.ToLowerInvariant();
        var markers = new[]
        {
            "search this page for ",
            "search page for ",
            "filter this page for ",
            "find on this page ",
            "search current page for ",
            "page search "
        };

        foreach (var marker in markers)
        {
            var index = lower.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0)
            {
                return clean[(index + marker.Length)..].Trim();
            }
        }

        if (lower is "search this page" or "search page" or "filter this page" or "page search")
        {
            return string.Empty;
        }

        return null;
    }

    private static bool IsSupportIntent(string normalized)
    {
        return normalized.Contains("support") ||
            normalized.Contains("contact") ||
            normalized.Contains("helpdesk") ||
            normalized.Contains("customer care") ||
            normalized.Contains("call") ||
            normalized.Contains("mobile") ||
            normalized.Contains("phone") ||
            normalized.Contains("email");
    }

    private static bool IsProactiveIntent(string normalized)
    {
        return normalized.Contains("what should i do") ||
            normalized.Contains("suggest") ||
            normalized.Contains("recommend") ||
            normalized.Contains("next step") ||
            normalized.Contains("guide") ||
            normalized.Contains("this page") ||
            normalized.Contains("smart") ||
            normalized.Contains("proactive") ||
            normalized.Contains("alert") ||
            normalized.Contains("risk") ||
            normalized.Contains("priority");
    }

    private static bool IsAdminOverviewIntent(string normalized)
    {
        return normalized.Contains("admin overview") ||
            normalized.Contains("admin summary") ||
            normalized.Contains("dashboard summary") ||
            normalized.Contains("operation summary") ||
            normalized.Contains("operations summary") ||
            normalized.Contains("report") ||
            normalized.Contains("alerts") ||
            normalized.Contains("priority");
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

    private string GetCurrentPath()
    {
        var referer = HttpContext.Request.Headers.Referer.ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath;
        }

        return HttpContext.Request.Path.Value ?? string.Empty;
    }

    private string GetCurrentPageName()
    {
        var path = GetCurrentPath().Trim('/');
        if (string.IsNullOrWhiteSpace(path))
        {
            return "the home page";
        }

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var last = parts.LastOrDefault() ?? path;

        return last
            .Replace("-", " ")
            .Replace("_", " ");
    }

    private static AmaroQuickOption[] FilterExportOptions(
        bool canExport,
        IEnumerable<AmaroQuickOption> options)
    {
        if (canExport)
        {
            return options.ToArray();
        }

        return options
            .Where(option =>
                !string.Equals(option.Command, "admin-export", StringComparison.OrdinalIgnoreCase) &&
                !option.Label.Contains("export", StringComparison.OrdinalIgnoreCase) &&
                !option.Url.Contains("/Export", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private record AmaroMenuItem(string? MenuCode, string? MenuName, string? RoutePath);
    public record AmaroAskRequest(string? Message);
    public record AmaroQuickOption(string Label, string Url, string? Command = null);
    public record AmaroAskResponse(string Message, AmaroQuickOption[] Options);
}
