using AmarShowsBook.Data;
using AmarShowsBook.Models.Admin;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using AmarShowsBook.Helpers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers
{
    public class AdminController : Controller
    {
//         // =========================================================
// // ADMIN TRANSACTION PAGE
// // Enterprise Transaction Monitoring
// // =========================================================

// public IActionResult Transactions(int page = 1)
// {
//     const int pageSize = 50;

//     var query = _context
//         .Set<AdminTransactionViewModel>()
//         .FromSqlRaw(@"
//             SELECT *
//             FROM vw_admin_transaction_complete
//             ORDER BY created_at DESC
//         ");

//     var totalCount = query.Count();

//     var transactions = query
//         .Skip((page - 1) * pageSize)
//         .Take(pageSize)
//         .ToList();

//     ViewBag.CurrentPage = page;

//     ViewBag.TotalPages =
//         (int)Math.Ceiling(totalCount / (double)pageSize);

//     return View(transactions);
// }
        // =====================================================
        // DATABASE CONTEXT
        // =====================================================

        private readonly ApplicationDbContext _context;

        // =====================================================
        // ACTIVITY LOGGER
        // =====================================================

        private readonly IActivityLogger _activityLogger;

        // =====================================================
        // ROLE BASED ACCESS SERVICE
        // =====================================================

        private readonly RbacService _rbacService;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        // Human Comment:
        // Injecting:
        // 1. Database access
        // 2. Activity logging service
        // 3. RBAC permission validation service

        public AdminController(
            ApplicationDbContext context,
            IActivityLogger activityLogger,
            RbacService rbacService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _rbacService = rbacService;
        }

        // =====================================================
        // ADMIN DASHBOARD
        // =====================================================

        public async Task<IActionResult> Dashboard()
        {
            // =====================================================
            // ROLE ACCESS VALIDATION
            // =====================================================

            // Human Comment:
            // Only users with ADMIN VIEW permission
            // can access admin dashboard

            if (!RbacAuthorizationHelper.CanAccess(
                HttpContext,
                _rbacService,
                "ADMIN",
                "VIEW"))
            {
                return RedirectToAction("Index", "Home");
            }

            // =====================================================
            // LOAD DASHBOARD COUNTS
            // =====================================================

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
// Using mapped C# property name.
// EF Core automatically maps this
// to PostgreSQL column "is_refund_error".

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

            // =====================================================
            // ACTIVITY LOG
            // =====================================================

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

        // =====================================================
        // ROLES PAGE
        // =====================================================

        // Human Comment:
        // Admin role management screen

        public async Task<IActionResult> Roles()
        {
            return View();
        }

        // =====================================================
        // PERMISSIONS PAGE
        // =====================================================

        // Human Comment:
        // Permission master screen

        public async Task<IActionResult> Permissions()
        {
            return View();
        }

        // =====================================================
        // USER ACCESS PAGE
        // =====================================================

        // Human Comment:
        // Shows role + permission matrix

        public async Task<IActionResult> UserAccess()
        {
            return View();
        }

        // =====================================================
        // USERS PAGE
        // =====================================================

        public IActionResult Users()
        {
            // Human Comment:
            // Load all users for admin management

            var users = _context.Users.ToList();

            return View(users);
        }

        // =====================================================
        // BOOKINGS PAGE
        // =====================================================

        public IActionResult Bookings()
        {
            // Human Comment:
            // Load booking summary data

            var bookings =
                _context.VwBookingCompleteDetails.ToList();

            return View(bookings);
        }

        // =====================================================
        // TRANSACTIONS PAGE
        // =====================================================

        public IActionResult Transactions()
        {
            // Human Comment:
            // Load payment transaction summaries

            var transactions =
                _context.VwBookingTransactionSummaries.ToList();

            return View(transactions);
        }

        // =====================================================
        // ACCESS MANAGEMENT PAGE
        // =====================================================

        public IActionResult AccessManagement()
        {
            // Human Comment:
            // Load user access matrix

            var access =
                _context.VwUserAccessMatrices.ToList();

            return View(access);
        }

        // =====================================================
        // MENUS PAGE
        // =====================================================

        public IActionResult Menus()
        {
            // Human Comment:
            // Load role based menus

            var menus =
                _context.VwUserApplicationMenus.ToList();

            return View(menus);
        }

        // =====================================================
        // REFUNDS PAGE
        // =====================================================

        public async Task<IActionResult> Refunds()
        {
            try
            {
                var refunds =
                    await _context.VwRefundSummaries
                        .ToListAsync();

                return View(refunds);
            }
            catch
            {
                // Human Comment:
                // Return empty list if database fails

                return View(new List<VwRefundSummary>());
            }
        }

        // =====================================================
        // WALLETS PAGE
        // =====================================================

        public async Task<IActionResult> Wallets()
        {
            try
            {
                var wallets =
                    await _context.VwWalletSummaries
                        .ToListAsync();

                return View(wallets);
            }
            catch
            {
                // Human Comment:
                // Return empty list if wallet query fails

                return View(new List<VwWalletSummary>());
            }
        }

        // =====================================================
        // NOTIFICATIONS PAGE
        // =====================================================

        public async Task<IActionResult> Notifications()
        {
            try
            {
                var notifications =
                    await _context.VwNotificationCenters
                        .ToListAsync();

                return View(notifications);
            }
            catch
            {
                // IMPORTANT FIX:
                // Your previous code used:
                // List<VwNotificationCenters>()
                // That class DOES NOT exist

                return View(new List<VwNotificationCenter>());
            }
        }

        // =====================================================
        // ACTIVITY LOGS PAGE
        // =====================================================

        public async Task<IActionResult> ActivityLogs()
        {
            try
            {
                // Human Comment:
                // Load latest 100 logs

                var logs =
                    await _context.ActivityLogs
                        .OrderByDescending(x => x.CreatedAt)
                        .Take(100)
                        .ToListAsync();

                return View(logs);
            }
            catch
            {
                return View(new List<ActivityLog>());
            }
        }
    }
}