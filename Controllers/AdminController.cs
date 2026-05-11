using AmarShowsBook.Data;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Helpers;

namespace AmarShowsBook.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IActivityLogger _activityLogger;
        private readonly RbacService _rbacService;

        // Inject database + activity logger
        public AdminController(
            ApplicationDbContext context,
            //IActivityLogger activityLogger)
            IActivityLogger activityLogger,
RbacService rbacService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _rbacService = rbacService;
        }

        // ================= ADMIN DASHBOARD =================

        public async Task<IActionResult> Dashboard()
        {
            // Validate admin dashboard permission
if (!RbacAuthorizationHelper.CanAccess(
    HttpContext,
    _rbacService,
    "ADMIN",
    "VIEW"))
{
    return RedirectToAction("Index", "Home");
}
            var vm = new AdminDashboardViewModel
            {
                // ================= BOOKINGS =================

                TotalBookings =
                    _context.VwBookingCompleteDetails.Count(),

                FailedBookings =
                    _context.VwBookingCompleteDetails
                        .Count(x => x.IsError == 1),

                // ================= PAYMENTS =================

                SuccessfulPayments =
                    _context.VwBookingTransactionSummaries
                        .Count(x => x.IsPaymentError == 0),

                FailedPayments =
                    _context.VwBookingTransactionSummaries
                        .Count(x => x.IsPaymentError == 1),

                // ================= REFUNDS =================

                TotalRefunds =
                    _context.VwRefundSummaries.Count(),

                FailedRefunds =
                    _context.VwRefundSummaries
                        .Count(x => x.IsRefundError == 1),

                // ================= INVOICES =================

                InvoiceFailures =
                    _context.VwInvoiceSummaries
                        .Count(x => x.IsInvoiceError == 1),

                // ================= NOTIFICATIONS =================

                NotificationFailures =
                    _context.VwNotificationCenters
                        .Count(x => x.IsError == 1),

                // ================= SECURITY =================

                TicketValidationIssues =
                    _context.VwTicketValidationSummaries
                        .Count(x => x.IsSecurityIssue == 1),

                // ================= WALLET =================

                TotalWalletBalance =
                    _context.VwWalletSummaries
                        .Sum(x => x.WalletBalance),

                TotalCredits =
                    _context.VwWalletSummaries
                        .Sum(x => x.TotalCredits),

                TotalDebits =
                    _context.VwWalletSummaries
                        .Sum(x => x.TotalDebits)
            };

            // Log admin dashboard access
            await _activityLogger.LogAsync(
                action: "VIEW_ADMIN_DASHBOARD",
                module: "ADMIN",
                entityType: "DASHBOARD",
                description: "Admin dashboard viewed",
                status: "SUCCESS",
                isError: 0
            );

            return View(vm);
        }


        }

        // =====================================================

        // USERS PAGE

        // =====================================================

        public async Task<IActionResult> Users()

        {

            var users = await _context.Users

                .OrderByDescending(x => x.CreatedAt)

                .ToListAsync();

            return View(users);

        }

        // =====================================================

        // USER DETAILS

        // =====================================================

        public async Task<IActionResult> UserDetails(int id)

        {

            var user = await _context.Users

                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)

            {

                TempData["Error"] = "User not found";

                return RedirectToAction("Users");

            }

            return View(user);

        }

        // =====================================================

        // DISABLE USER

        // =====================================================

        [HttpPost]

        public async Task<IActionResult> DisableUser(int id)

        {

            var user = await _context.Users

                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)

            {

                TempData["Error"] = "User not found";

                return RedirectToAction("Users");

            }

            user.IsActive = false;

            user.UpdatedAt = DateTime.UtcNow;

            user.UpdatedBy = "ADMIN";

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(

                userId: user.Id,

                action: "DISABLE_USER",

                module: "ADMIN",

                entityType: "USER",

                entityId: user.Id,

                description: "Admin disabled user account",

                status: "SUCCESS"

            );

            TempData["Success"] = "User disabled successfully";

            return RedirectToAction("Users");

        }

        // =====================================================

        // ENABLE USER

        // =====================================================

        [HttpPost]

        public async Task<IActionResult> EnableUser(int id)

        {

            var user = await _context.Users

                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)

            {

                TempData["Error"] = "User not found";

                return RedirectToAction("Users");

            }

            user.IsActive = true;

            user.UpdatedAt = DateTime.UtcNow;

            user.UpdatedBy = "ADMIN";

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(

                userId: user.Id,

                action: "ENABLE_USER",

                module: "ADMIN",

                entityType: "USER",

                entityId: user.Id,

                description: "Admin enabled user account",

                status: "SUCCESS"

            );

            TempData["Success"] = "User enabled successfully";

            return RedirectToAction("Users");

        }

        // =====================================================

        // SOFT DELETE USER

        // =====================================================

        [HttpPost]

        public async Task<IActionResult> DeleteUser(int id)

        {

            var user = await _context.Users

                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)

            {

                TempData["Error"] = "User not found";

                return RedirectToAction("Users");

            }

            user.IsDeleted = true;

            user.IsActive = false;

            user.UpdatedAt = DateTime.UtcNow;

            user.UpdatedBy = "ADMIN";

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(

                userId: user.Id,

                action: "DELETE_USER",

                module: "ADMIN",

                entityType: "USER",

                entityId: user.Id,

                description: "Admin soft deleted user",

                status: "SUCCESS"

            );

            TempData["Success"] = "User deleted successfully";

            return RedirectToAction("Users");

        }

        // =====================================================

        // BOOKINGS

        // =====================================================

        public async Task<IActionResult> Bookings()

        {

            var bookings =

                await _context

                    .Set<VwBookingCompleteDetails>()

                    .ToListAsync();

            return View(bookings);

        }

        // =====================================================

        // TRANSACTIONS

        // =====================================================

        public async Task<IActionResult> Transactions()

        {

            var transactions =

                await _context

                    .Set<VwBookingTransactionSummary>()

                    .ToListAsync();

            return View(transactions);

        }

        // =====================================================

        // REFUNDS

        // =====================================================

        public async Task<IActionResult> Refunds()

        {

            var refunds =

                await _context

                    .Set<VwRefundSummary>()

                    .ToListAsync();

            return View(refunds);

        }

        // =====================================================

        // WALLET SUMMARY

        // =====================================================

        public async Task<IActionResult> Wallets()

        {

            var wallets =

                await _context

                    .Set<VwWalletSummary>()

                    .ToListAsync();

            return View(wallets);

        }

        // =====================================================

        // NOTIFICATIONS

        // =====================================================

        public async Task<IActionResult> Notifications()

        {

            var notifications =

                await _context

                    .Set<VwNotificationCenter>()

                    .ToListAsync();

            return View(notifications);

        }

        // =====================================================

        // ACTIVITY LOGS

        // =====================================================

        public async Task<IActionResult> ActivityLogs()

        {

            var logs =

                await _context.ActivityLogs

                    .OrderByDescending(x => x.CreatedAt)

                    .Take(500)

                    .ToListAsync();

            return View(logs);

        }

    }

}