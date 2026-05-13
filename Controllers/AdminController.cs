using AmarShowsBook.Data;
using AmarShowsBook.Models;
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
            // Load non-deleted users for admin management

          var users = _context.Users
    .AsNoTracking()
    .OrderByDescending(x => x.CreatedAt)
    .ToList();

            return View(users);
        }

        // =====================================================
        // ADMIN USER STATUS ACTIONS
        // =====================================================

        [HttpPost]
        public async Task<IActionResult> DisableUser(int id)
        {
            // Human Comment:
            // Soft-disable keeps the user record while blocking account use.

            var user = await _context.Users.FindAsync(id);

            if (user != null)
            {
                user.is_active = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> EnableUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user != null && !user.is_deleted)
            {
                user.is_active = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Users));
        }

        // =====================================================
        // BOOKINGS PAGE
        // =====================================================

        public IActionResult Bookings()
        {
            // Human Comment:
            // Load booking summary data

            var bookings =
                _context.VwBookingCompleteDetails
                    .AsNoTracking()
                    .ToList();

            return View(bookings);
        }

        // =====================================================
        // TRANSACTIONS PAGE
        // =====================================================

        public async Task<IActionResult> Transactions(int page = 1)
        {
            // Human Comment:
            // Show 50 transaction rows per page for admin readability.

            const int pageSize = 50;

            page = Math.Max(page, 1);

            var query = _context.VwBookingTransactionSummaries
                .AsNoTracking()
                .OrderByDescending(x => x.BookingCreatedAt);

            // Human Comment:
            // One aggregate query replaces three separate full scans of the transaction view.
            var summary = await _context.VwBookingTransactionSummaries
                .AsNoTracking()
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Success = g.Count(x => x.TransactionStatus == "SUCCESS"),
                    Failed = g.Count(x => x.TransactionStatus != "SUCCESS")
                })
                .FirstOrDefaultAsync();

            var totalCount = summary?.Total ?? 0;

            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            ViewBag.TotalRecords = totalCount;
            ViewBag.SuccessCount = summary?.Success ?? 0;
            ViewBag.FailedCount = summary?.Failed ?? 0;

            return View(transactions);
        }

        // =====================================================
        // TRANSACTION DETAILS PAGE
        // =====================================================

        public async Task<IActionResult> TransactionDetails(int id)
        {
            // Human Comment:
            // Opens the transaction View button from the admin transaction table.

            var transaction = await _context.VwBookingTransactionSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TransactionId == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // =====================================================
        // ACCESS MANAGEMENT PAGE
        // =====================================================

        public IActionResult AccessManagement()
        {
            // Human Comment:
            // Load user access matrix

            var access =
                _context.VwUserAccessMatrices
                    .AsNoTracking()
                    .ToList();

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
                _context.VwUserApplicationMenus
                    .AsNoTracking()
                    .ToList();

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
                        .AsNoTracking()
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
                        .AsNoTracking()
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
                        .AsNoTracking()
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
                        .AsNoTracking()
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
        // =====================================================
// HUMAN COMMENT:
// ADMIN USER DETAILS PAGE
// =====================================================

// =====================================================
// HUMAN COMMENT:
// FULL ADMIN USER DETAILS PAGE
// =====================================================

// =====================================================
// HUMAN COMMENT:
// FULL ADMIN USER DETAILS PAGE
// =====================================================

public IActionResult UserDetails(long id)
{
    var user = _context.Users
        .FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return NotFound();
    }

    // =====================================================
    // HUMAN COMMENT:
    // LOAD LAST 5 TRANSACTIONS
    // =====================================================

    var transactions = _context.VwBookingTransactionSummaries
        .Where(x => x.UserId == id)
        .OrderByDescending(x => x.BookingCreatedAt)
        .Take(5)
        .ToList();

    // =====================================================
    // HUMAN COMMENT:
    // CALCULATE TRANSACTION STATS
    // =====================================================

    var successCount =
        transactions.Count(x =>
            x.TransactionStatus == "SUCCESS");

    var failedCount =
        transactions.Count(x =>
            x.TransactionStatus == "FAILED");

    var pendingCount =
        transactions.Count(x =>
            x.TransactionStatus == "PENDING");

var totalSpent =
    transactions
        .Where(x =>
            x.TransactionStatus == "SUCCESS")
        .Sum(x =>
            x.TransactionAmount ?? 0);

    var lastTransaction =
        transactions.FirstOrDefault();

    // =====================================================
    // HUMAN COMMENT:
    // LOAD WALLET DATA
    // =====================================================

    var wallet = _context.VwWalletSummaries
        .FirstOrDefault(x => x.UserId == id);

    // =====================================================
    // HUMAN COMMENT:
    // RECENT ACTIVITY MOCK DATA
    // =====================================================

    var recentActivities = new List<string>
    {
        "User logged in",
        "Updated profile",
        "Booked movie ticket",
        "Payment completed",
        "Viewed profile"
    };

    // =====================================================
    // HUMAN COMMENT:
    // CREATE VIEW MODEL
    // =====================================================

    var model = new AdminUserDetailsViewModel
    {
        UserId = user.Id,

        Name = user.Name,
        Email = user.Email,
        Mobile = user.Mobile,

        Language = user.Language,
        Genre = user.Genre,

        Country = user.Country,
        State = user.State,
        District = user.District,

        Address = user.Address,
        Pincode = user.Pincode,

        // =====================================================
        // IMPORTANT FIX:
        // NULL SAFE IMAGE
        // =====================================================

        ProfileImagePath =
            string.IsNullOrWhiteSpace(user.ProfileImagePath)
                ? "/images/default-user.png"
                : user.ProfileImagePath,

        IsActive = user.is_active,
        IsDeleted = user.is_deleted,

        RegisteredAt = user.CreatedAt,

        LastLoginAt = user.UpdatedAt,

        // =====================================================
        // WALLET
        // =====================================================

        WalletBalance =
            wallet?.WalletBalance ?? 0,

        // =====================================================
        // TRANSACTION SUMMARY
        // =====================================================

        TotalTransactions = transactions.Count,

        SuccessTransactions = successCount,

        FailedTransactions = failedCount,

        PendingTransactions = pendingCount,

        TotalSpent = totalSpent,

        LastTransactionRef =
            lastTransaction?.TransactionRef ?? "-",

        LastTransactionStatus =
            lastTransaction?.TransactionStatus ?? "-",

        LastTransactionDate =
            lastTransaction?.BookingCreatedAt,

        // =====================================================
        // LAST 5 TRANSACTIONS
        // =====================================================

        LastTransactions = transactions,

        // =====================================================
        // ACTIVITIES
        // =====================================================

        RecentActivities = recentActivities
    };

    return View(model);
}
// =====================================================
// HUMAN COMMENT:
// TOGGLE USER ACTIVE STATUS
// =====================================================

public IActionResult ToggleUserStatus(long id)
{
    var user = _context.Users.FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return NotFound();
    }

    user.is_active = !user.is_active;

    _context.SaveChanges();

    return RedirectToAction("Users");
}
// =====================================================
// HUMAN COMMENT:
// SOFT DELETE USER
// USER MOVES TO deleted_users TABLE
// =====================================================

public IActionResult DeleteUser(int id)
{
    var user = _context.Users.FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return RedirectToAction("Users");
    }

    // =====================================================
    // HUMAN COMMENT:
    // STORE USER SNAPSHOT IN deleted_users TABLE
    // =====================================================

    var deletedUser = new DeletedUser
    {
        original_user_id = user.Id,

        name = user.Name,
        email = user.Email,
        mobile = user.Mobile,

        address = user.Address,

        country = user.Country,
        state = user.State,
        district = user.District,
        pincode = user.Pincode,

        language = user.Language,
        genre = user.Genre,

        profile_image_path = user.ProfileImagePath,

        created_at = user.CreatedAt,
        updated_at = user.UpdatedAt,

        deleted_at = DateTime.UtcNow,

        deleted_by = HttpContext.Session.GetString("UserName"),

        is_revoked = false
    };
// =====================================================
// HUMAN COMMENT:
// SAVE USER SNAPSHOT INTO deleted_users TABLE
// =====================================================

_context.DeletedUsers.Add(deletedUser);

// =====================================================
// HUMAN COMMENT:
// MARK USER AS DELETED
// =====================================================

user.is_deleted = true;
user.is_active = false;

user.UpdatedAt = DateTime.UtcNow;

// =====================================================
// HUMAN COMMENT:
// SAVE ALL CHANGES
// =====================================================

_context.SaveChanges();

    return RedirectToAction("Users");
}
// =====================================================
// HUMAN COMMENT:
// RESTORE USER FROM DELETED STATE
// =====================================================

// =====================================================
// HUMAN COMMENT:
// REVOKE USER FROM deleted_users TABLE
// =====================================================

public IActionResult RevokeUser(int id)
{
    var user = _context.Users.FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return RedirectToAction("Users");
    }

    user.is_deleted = false;
    user.is_active = true;

    // =====================================================
    // HUMAN COMMENT:
    // UPDATE deleted_users TABLE
    // =====================================================

    var deletedRecord = _context.DeletedUsers
        .FirstOrDefault(x =>
            x.original_user_id == user.Id &&
            x.is_revoked == false);

    if (deletedRecord != null)
    {
        deletedRecord.is_revoked = true;

        deletedRecord.revoke_at = DateTime.UtcNow;

        deletedRecord.revoked_by =
            HttpContext.Session.GetString("UserName");
    }

    _context.SaveChanges();

    return RedirectToAction("Users");
}

    }
}
