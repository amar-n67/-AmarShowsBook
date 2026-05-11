using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;

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

            // Prevent guest users from accessing transactions
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            var transactions = _context
                .VwBookingTransactionSummaries
                .Where(x => x.UserEmail == userEmail)
                .OrderByDescending(x => x.BookingCreatedAt)
                .ToList();

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
    }
}