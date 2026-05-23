using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        // Inject database context and activity logger
        public TransactionController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // ================= TRANSACTION HISTORY =================

        public async Task<IActionResult> History()
        {
            // Get logged-in user email from session
            var userEmail =
                HttpContext.Session.GetString("UserEmail");
            var userIdText =
                HttpContext.Session.GetString("UserId");

            // Prevent guest users from accessing transactions
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Human Comment:
            // User id is the stable lookup key; email remains as a compatibility fallback.
            var userId = long.TryParse(userIdText, out var parsedUserId)
                ? parsedUserId
                : 0;

            var transactions = await _context
                .VwBookingTransactionSummaries
                .AsNoTracking()
                .Where(x =>
                    x.TransactionId != null &&
                    ((userId > 0 && x.UserId == userId) ||
                    x.UserEmail.ToLower() == userEmail.ToLower()))
                .OrderByDescending(x => x.BookingCreatedAt)
                .ToListAsync();

            // Log successful transaction page access
            await _activityLogger.LogAsync(
                action: "VIEW_TRANSACTIONS",
                module: "TRANSACTION",
                entityType: "TRANSACTION",
                description: "User viewed transaction history",
                status: "SUCCESS",
                isError: 0,
                metadata: new
                {
                    TotalTransactions = transactions.Count
                }
            );

            return View(transactions);
        }

        public async Task<IActionResult> Details(long id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userIdText = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            var userId = long.TryParse(userIdText, out var parsedUserId)
                ? parsedUserId
                : 0;

            var transaction = await _context
                .VwBookingTransactionSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.TransactionId == id &&
                    ((userId > 0 && x.UserId == userId) ||
                    x.UserEmail.ToLower() == userEmail.ToLower()));

            if (transaction == null)
            {
                return NotFound();
            }

            await _activityLogger.LogAsync(
                action: "VIEW_TRANSACTION_DETAILS",
                module: "TRANSACTION",
                entityType: "TRANSACTION",
                entityId: id <= int.MaxValue ? (int)id : null,
                description: "User viewed transaction details",
                status: "SUCCESS",
                isError: 0
            );

            return View(transaction);
        }
    }
}
