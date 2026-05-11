using AmarShowsBook.Data;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Helpers;
using AmarShowsBook.Models.Admin;
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
        // =====================================================
// ROLES
// =====================================================

// Human Comment:
// Admin can manage roles

public IActionResult Roles()
{
    var roles = _context.Roles.ToList();

    return View(roles);
}

// =====================================================
// PERMISSIONS
// =====================================================

// Human Comment:
// Admin can manage permissions

public IActionResult Permissions()
{
    var permissions = _context.Permissions.ToList();

    return View(permissions);
}
        // ================= USERS =================

public IActionResult Users()
{
    
    // Load all users for admin management page

    var users = _context.Users.ToList();

    return View(users);
}

// ================= BOOKINGS =================

public IActionResult Bookings()
{
    
    // Load booking summary data from database view

    var bookings =
        _context.VwBookingCompleteDetails.ToList();

    return View(bookings);
}

// ================= TRANSACTIONS =================

public IActionResult Transactions()
{
    
    // Load payment transaction summaries

    var transactions =
        _context.VwBookingTransactionSummaries.ToList();

    return View(transactions);
}

// ================= REFUNDS =================

public IActionResult Refunds()
{
    
    // Load refund analytics data

    var refunds =
        _context.VwRefundSummaries.ToList();

    return View(refunds);
}

// ================= WALLETS =================

public IActionResult Wallets()
{
    
    // Load wallet summaries

    var wallets =
        _context.VwWalletSummaries.ToList();

    return View(wallets);
}

// ================= NOTIFICATIONS =================

public IActionResult Notifications()
{
    
    // Load notification delivery status

    var notifications =
        _context.VwNotificationCenters.ToList();

    return View(notifications);
}

// ================= ACCESS MANAGEMENT =================

public IActionResult AccessManagement()
{
    
    // Load user role + permission matrix

    var access =
        _context.VwUserAccessMatrices.ToList();

    return View(access);
}

// ================= MENUS =================

public IActionResult Menus()
{
    
    // Load role based menu access

    var menus =
        _context.VwUserApplicationMenus.ToList();

    return View(menus);
}


// Admin can view all registered users

// =====================================================
// USER ACCESS MATRIX
// =====================================================


// Admin can view RBAC access matrix

public IActionResult UserAccess()
{
    var accessList = _context.VwUserAccessMatrices.ToList();

    return View(accessList);
}


// =====================================================
// ACTIVITY LOGS
// =====================================================


// Admin activity audit logs

public IActionResult ActivityLogs()
{
    var logs = _context.ActivityLogs
        .OrderByDescending(x => x.CreatedAt)
        .ToList();

    return View(logs);
}


    }
}