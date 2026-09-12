using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers
{
    // Customer transaction pages always filter by the signed-in user before showing payment history or details.
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public TransactionController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }


        public async Task<IActionResult> History()
        {
            var userEmail =
                HttpContext.Session.GetString("UserEmail");
            var userIdText =
                HttpContext.Session.GetString("UserId");

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

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
