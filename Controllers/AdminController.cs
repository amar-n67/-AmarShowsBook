using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.Admin;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using AmarShowsBook.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers
{
    public class AdminController : Controller
    {
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

     public IActionResult Roles()
        {
            return View();
        }

        // =====================================================
        // PERMISSIONS PAGE
        // =====================================================

        // Human Comment:
        // Permission master screen

      public IActionResult Permissions()
        {
            return View();
        }

        public IActionResult Users(int page = 1)
        {
            // Human Comment:
            // Load users in 50-row pages so the admin table stays fast and readable.

            const int pageSize = 50;
            page = Math.Max(page, 1);

            var query = _context.Users
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = query.Count();

            var users = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            ViewBag.TotalRecords = totalCount;

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

        public IActionResult Bookings(int page = 1)
        {
            // Human Comment:
            // Load booking summary data in 50-row pages for admin tables.

            const int pageSize = 50;
            page = Math.Max(page, 1);

            var query =
                _context.VwBookingCompleteDetails
                    .AsNoTracking()
                    .OrderByDescending(x => x.BookedAt);

            var totalCount = query.Count();

            var bookings = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            ViewBag.TotalRecords = totalCount;

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
        // ACTIVITY LOGS PAGE
        // =====================================================

        public IActionResult ActivityLogs(int page = 1)
        {
            // Human Comment:
            // Activity logs use the same 50-row admin pagination pattern as transactions.

            const int pageSize = 50;
            page = Math.Max(page, 1);

            var query = _context
                .VwEnterpriseActivityLogs
                .AsNoTracking()
                .OrderByDescending(x => x.activity_time);

            var totalCount = query.Count();

            var logs = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            ViewBag.TotalRecords = totalCount;

            return View(logs);
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

        public async Task<IActionResult> Refunds(int page = 1)
        {
            try
            {
                // Human Comment:
                // Refunds follow the shared 50-row admin page size while keeping page layout intact.
                const int pageSize = 50;
                page = Math.Max(page, 1);

                var query = _context.VwRefundSummaries
                    .AsNoTracking()
                    .OrderByDescending(x => x.RequestedAt ?? x.CreatedAt);

                var totalCount = await query.CountAsync();

                var refunds = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                ViewBag.TotalRecords = totalCount;

                return View(refunds);
            }
            catch
            {
                // Human Comment:
                // Return empty list if database fails

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalRecords = 0;

                return View(new List<VwRefundSummary>());
            }
        }

        // =====================================================
        // WALLETS PAGE
        // =====================================================

        public async Task<IActionResult> Wallets(int page = 1)
        {
            try
            {
                // Human Comment:
                // Wallet admin page uses the same 50-row paging contract as other admin lists.
                const int pageSize = 50;
                page = Math.Max(page, 1);

                var query = _context.VwWalletSummaries
                    .AsNoTracking()
                    .OrderBy(x => x.UserName);

                var totalCount = await query.CountAsync();

                var wallets = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                ViewBag.TotalRecords = totalCount;

                return View(wallets);
            }
            catch
            {
                // Human Comment:
                // Return empty list if wallet query fails

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalRecords = 0;

                return View(new List<VwWalletSummary>());
            }
        }

        // =====================================================
        // NOTIFICATIONS PAGE
        // =====================================================

        public async Task<IActionResult> Notifications(int page = 1)
        {
            try
            {
                // Human Comment:
                // Notification admin page uses the shared 50-row pagination contract.
                const int pageSize = 50;
                page = Math.Max(page, 1);

                var query = _context.VwNotificationCenters
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt);

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                ViewBag.TotalRecords = totalCount;

                return View(notifications);
            }
            catch
            {
                // IMPORTANT FIX:
                // Your previous code used:
                // List<VwNotificationCenters>()
                // That class DOES NOT exist

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalRecords = 0;

                return View(new List<VwNotificationCenter>());
            }
        }
// public async Task<IActionResult> RefundDetails(long id)
// {
//     var refund = await _context
//         .VwRefundSummaries
//         .AsNoTracking()
//         .FirstOrDefaultAsync(x => x.RefundId == id);

//     if (refund == null)
//     {
//         return NotFound();
//     }

//     return View(refund);
// }
// public IActionResult ActivityLogs(int page = 1)
// {
//     int pageSize = 50;

//     var query = _context
//         .VwEnterpriseActivityLogs
//         .AsNoTracking()
//         .OrderByDescending(x => x.activity_time);

//     int totalCount = query.Count();

//     var logs = query
//         .Skip((page - 1) * pageSize)
//         .Take(pageSize)
//         .ToList();

//     ViewBag.CurrentPage = page;

//     ViewBag.TotalPages =
//         (int)Math.Ceiling(
//             totalCount / (double)pageSize);

//     return View(logs);
// }
// // =====================================================
// // APPROVE REFUND
// // =====================================================

// [HttpPost]
// public async Task<IActionResult> ApproveRefund(long id)
// {
//     var refund = await _context.Refunds
//         .FirstOrDefaultAsync(x => x.id == id);

//     if (refund == null)
//     {

//         // =====================================================
// // RBAC VALIDATION
// // =====================================================

// if (!RbacAuthorizationHelper.CanAccess(
//     HttpContext,
//     _rbacService,
//     "REFUND",
//     "APPROVE"))
// {
//     TempData["Error"] =
//         "You do not have permission to approve refunds.";

//     return RedirectToAction("Refunds");
// }

//         TempData["Error"] =
//             "Refund not found.";

//         return RedirectToAction("Refunds");
//     }

//     // =====================================================
//     // UPDATE STATUS
//     // =====================================================

//     refund.refund_status = "APPROVED";

//     refund.workflow_action =
//         "APPROVED BY ADMIN";

//     refund.processed_at =
//         DateTime.UtcNow;

//     refund.updated_at =
//         DateTime.UtcNow;

//     // =====================================================
//     // ADMIN DETAILS
//     // =====================================================

//     refund.approved_by =
//         HttpContext.Session.GetString("UserName");

//     refund.approved_at =
//         DateTime.UtcNow;
//         refund.admin_notes =
//     "Refund approved by admin";

// refund.workflow_action =
//     "APPROVED BY ADMIN";

// refund.approved_by =
//     HttpContext.Session.GetString("UserName");

// refund.approved_at =
//     DateTime.UtcNow;

//     // =====================================================
//     // SAVE
//     // =====================================================

//     await _context.SaveChangesAsync();

//     // =====================================================
//     // ACTIVITY LOG
//     // =====================================================
// _context.RefundActionLogs.Add(
//     new RefundActionLog
//     {
//         refund_id = refund.id,

//         refund_ref = refund.refund_ref,

//         action_name = "APPROVE_REFUND",

//         action_by =
//             HttpContext.Session.GetString("UserName"),

//         action_time = DateTime.UtcNow,

//         action_notes =
//             "Refund approved successfully",

//         ip_address =
//             HttpContext.Connection.RemoteIpAddress?.ToString(),

//         created_at = DateTime.UtcNow
//     });

// await _context.SaveChangesAsync();
//     await _activityLogger.LogAsync(
//         action: "APPROVE_REFUND",
//         module: "NOTIFICATION",
//         entityType: "REFUND",
//         description:
//             $"Refund approved: {refund.refund_ref}",
//         status: "SUCCESS",
//         isError: 0
//     );

//     TempData["Success"] =
//         "Refund approved successfully.";

//     return RedirectToAction("Refunds");
// }


// // =====================================================
// // REJECT REFUND
// // =====================================================

// [HttpPost]
// public async Task<IActionResult> RejectRefund(long id)
// {
//     var refund = await _context.Refunds
//         .FirstOrDefaultAsync(x => x.id == id);

//     if (refund == null)
//     {
//         TempData["Error"] =
//             "Refund not found.";

//         return RedirectToAction("Refunds");
//     }

//     // =====================================================
//     // UPDATE STATUS
//     // =====================================================

//     refund.refund_status = "REJECTED";

//     refund.workflow_action =
//         "REJECTED BY ADMIN";

//     refund.updated_at =
//         DateTime.UtcNow;

//     // =====================================================
//     // ADMIN DETAILS
//     // =====================================================

//     refund.rejected_by =
//         HttpContext.Session.GetString("UserName");

//     refund.rejected_at =
//         DateTime.UtcNow;

//     // =====================================================
//     // SAVE
//     // =====================================================

//     await _context.SaveChangesAsync();

//     // =====================================================
//     // ACTIVITY LOG
//     // =====================================================

//     await _activityLogger.LogAsync(
//         action: "REJECT_REFUND",
//         module: "REFUND",
//         entityType: "REFUND",
//         description:
//             $"Refund rejected: {refund.refund_ref}",
//         status: "SUCCESS",
//         isError: 0
//     );

//     TempData["Success"] =
//         "Refund rejected successfully.";

//     return RedirectToAction("Refunds");
// }


// // =====================================================
// // RETRY REFUND
// // =====================================================

// [HttpPost]
// public async Task<IActionResult> RetryRefund(long id)
// {
//     var refund = await _context.Refunds
//         .FirstOrDefaultAsync(x => x.id == id);

//     if (refund == null)
//     {
//         TempData["Error"] =
//             "Refund not found.";

//         return RedirectToAction("Refunds");
//     }

//     // =====================================================
//     // UPDATE STATUS
//     // =====================================================

//     refund.refund_status = "PROCESSING";

//     refund.workflow_action =
//         "RETRIED BY ADMIN";

//     refund.failure_reason = null;

//     refund.updated_at =
//         DateTime.UtcNow;

//     // =====================================================
//     // ADMIN DETAILS
//     // =====================================================

//     refund.retried_by =
//         HttpContext.Session.GetString("UserName");

//     refund.retried_at =
//         DateTime.UtcNow;

//     // =====================================================
//     // SAVE
//     // =====================================================

//     await _context.SaveChangesAsync();

//     // =====================================================
//     // ACTIVITY LOG
//     // =====================================================

//     await _activityLogger.LogAsync(
//         action: "RETRY_REFUND",
//         module: "REFUND",
//         entityType: "REFUND",
//         description:
//             $"Refund retry initiated: {refund.refund_ref}",
//         status: "SUCCESS",
//         isError: 0
//     );

//     TempData["Success"] =
//         "Refund retry initiated successfully.";

//     return RedirectToAction("Refunds");
// }

// [HttpPost]
// public async Task<IActionResult> SaveRefundNotes(
//     long refundId,
//     string notes)
// {
//     var refund = await _context.Refunds
//         .FirstOrDefaultAsync(x => x.id == refundId);

//     if (refund == null)
//     {
//         TempData["Error"] = "Refund not found.";

//         return RedirectToAction("Refunds");
//     }

//     refund.admin_notes = notes;

//     refund.updated_at = DateTime.UtcNow;

//     await _context.SaveChangesAsync();

//     await _activityLogger.LogAsync(
//         action: "SAVE_REFUND_NOTES",
//         module: "REFUND",
//         entityType: "REFUND",
//         description: $"Admin notes updated for {refund.refund_ref}",
//         status: "SUCCESS",
//         isError: 0
//     );

//     TempData["Success"] =
//         "Admin notes saved successfully.";

//     return RedirectToAction(
//         "RefundDetails",
//         new { id = refundId });
// }
// // =====================================================
// // EXPORT REFUNDS CSV
// // =====================================================

// public IActionResult ExportRefunds()
// {
//     var refunds = _context.VwRefundSummaries
//         .AsNoTracking()
//         .ToList();

//     var builder = new System.Text.StringBuilder();

//     // =====================================================
//     // CSV HEADER
//     // =====================================================

//     builder.AppendLine(
//         "RefundRef,BookingRef,TransactionRef,UserName,UserEmail,RefundAmount,RefundStatus,RefundMethod,RequestedAt");

//     // =====================================================
//     // CSV ROWS
//     // =====================================================

//     foreach (var item in refunds)
//     {
//         builder.AppendLine(
//             $"{item.RefundRef}," +
//             $"{item.BookingRef}," +
//             $"{item.TransactionRef}," +
//             $"{item.UserName}," +
//             $"{item.UserEmail}," +
//             $"{item.RefundAmount}," +
//             $"{item.RefundStatus}," +
//             $"{item.RefundMethod}," +
//             $"{item.RequestedAt}"
//         );
//     }

//     // =====================================================
//     // DOWNLOAD CSV FILE
//     // =====================================================

//     return File(
//         System.Text.Encoding.UTF8.GetBytes(builder.ToString()),
//         "text/csv",
//         $"refunds_{DateTime.Now:yyyyMMddHHmmss}.csv"
//     );
// }

// =====================================================
// REFUND DETAILS PAGE
// =====================================================

public async Task<IActionResult> RefundDetails(long id)
{
    var refund = await _context
        .VwRefundSummaries
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.RefundId == id);

    if (refund == null)
    {
        return NotFound();
    }

    return View(refund);
}

// =====================================================
// APPROVE REFUND
// =====================================================

[HttpPost]
public async Task<IActionResult> ApproveRefund(long id)
{
    // =====================================================
    // RBAC VALIDATION
    // =====================================================

    if (!RbacAuthorizationHelper.CanAccess(
        HttpContext,
        _rbacService,
        "REFUND",
        "APPROVE"))
    {
        TempData["Error"] =
            "You do not have permission to approve refunds.";

        return RedirectToAction("Refunds");
    }

    // =====================================================
    // LOAD REFUND
    // =====================================================

    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == id);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }

    // =====================================================
    // UPDATE REFUND STATUS
    // =====================================================

    refund.refund_status = "SUCCESS";

    refund.workflow_action =
        "APPROVED BY ADMIN";

    refund.processed_at =
        DateTime.UtcNow;

    refund.updated_at =
        DateTime.UtcNow;

    // =====================================================
    // ADMIN TRACKING
    // =====================================================

    refund.approved_by =
        HttpContext.Session.GetString("UserName");

    refund.approved_at =
        DateTime.UtcNow;

    refund.admin_notes =
        "Refund approved by admin";

    // =====================================================
    // SAVE AUDIT LOG
    // =====================================================

    _context.RefundActionLogs.Add(
        new RefundActionLog
        {
            refund_id = refund.id,

            refund_ref = refund.refund_ref,

            action_name = "APPROVE_REFUND",

            action_by =
                HttpContext.Session.GetString("UserName"),

            action_time =
                DateTime.UtcNow,

            action_notes =
                "Refund approved successfully",

            ip_address =
                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

            created_at =
                DateTime.UtcNow
        });

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    await _context.SaveChangesAsync();

    // =====================================================
    // ACTIVITY LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "APPROVE_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund approved: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    // =====================================================
    // NOTIFICATION LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "REFUND_NOTIFICATION",
        module: "NOTIFICATION",
        entityType: "REFUND",
        description:
            $"Approval notification sent for refund {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    TempData["Success"] =
        "Refund approved successfully.";

    return RedirectToAction("Refunds");
}

// =====================================================
// REJECT REFUND
// =====================================================

[HttpPost]
public async Task<IActionResult> RejectRefund(long id)
{
    // =====================================================
    // RBAC VALIDATION
    // =====================================================

    if (!RbacAuthorizationHelper.CanAccess(
        HttpContext,
        _rbacService,
        "REFUND",
        "REJECT"))
    {
        TempData["Error"] =
            "You do not have permission to reject refunds.";

        return RedirectToAction("Refunds");
    }

    // =====================================================
    // LOAD REFUND
    // =====================================================

    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == id);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }

    // =====================================================
    // UPDATE REFUND STATUS
    // =====================================================

    refund.refund_status = "REJECTED";

    refund.workflow_action =
        "REJECTED BY ADMIN";

    refund.updated_at =
        DateTime.UtcNow;

    // =====================================================
    // ADMIN TRACKING
    // =====================================================

    refund.rejected_by =
        HttpContext.Session.GetString("UserName");

    refund.rejected_at =
        DateTime.UtcNow;

    refund.admin_notes =
        "Refund rejected by admin";

    // =====================================================
    // SAVE AUDIT LOG
    // =====================================================

    _context.RefundActionLogs.Add(
        new RefundActionLog
        {
            refund_id = refund.id,

            refund_ref = refund.refund_ref,

            action_name = "REJECT_REFUND",

            action_by =
                HttpContext.Session.GetString("UserName"),

            action_time =
                DateTime.UtcNow,

            action_notes =
                "Refund rejected by admin",

            ip_address =
                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

            created_at =
                DateTime.UtcNow
        });

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    await _context.SaveChangesAsync();

    // =====================================================
    // ACTIVITY LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "REJECT_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund rejected: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    // =====================================================
    // NOTIFICATION LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "REFUND_NOTIFICATION",
        module: "NOTIFICATION",
        entityType: "REFUND",
        description:
            $"Rejection notification sent for refund {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    TempData["Success"] =
        "Refund rejected successfully.";

    return RedirectToAction("Refunds");
}

// =====================================================
// RETRY REFUND
// =====================================================

[HttpPost]
public async Task<IActionResult> RetryRefund(long id)
{
    // =====================================================
    // RBAC VALIDATION
    // =====================================================

    if (!RbacAuthorizationHelper.CanAccess(
        HttpContext,
        _rbacService,
        "REFUND",
        "RETRY"))
    {
        TempData["Error"] =
            "You do not have permission to retry refunds.";

        return RedirectToAction("Refunds");
    }

    // =====================================================
    // LOAD REFUND
    // =====================================================

    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == id);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }

    // =====================================================
    // RESET REFUND STATUS
    // =====================================================

    refund.refund_status = "PENDING";

    refund.workflow_action =
        "RETRIED BY ADMIN";

    refund.failure_reason = null;

    refund.updated_at =
        DateTime.UtcNow;

    // =====================================================
    // ADMIN TRACKING
    // =====================================================

    refund.retried_by =
        HttpContext.Session.GetString("UserName");

    refund.retried_at =
        DateTime.UtcNow;

    refund.admin_notes =
        "Refund retry initiated by admin";

    // =====================================================
    // SAVE AUDIT LOG
    // =====================================================

    _context.RefundActionLogs.Add(
        new RefundActionLog
        {
            refund_id = refund.id,

            refund_ref = refund.refund_ref,

            action_name = "RETRY_REFUND",

            action_by =
                HttpContext.Session.GetString("UserName"),

            action_time =
                DateTime.UtcNow,

            action_notes =
                "Refund retry initiated",

            ip_address =
                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

            created_at =
                DateTime.UtcNow
        });

    // =====================================================
    // SAVE CHANGES
    // =====================================================

    await _context.SaveChangesAsync();

    // =====================================================
    // ACTIVITY LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "RETRY_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund retry initiated: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    // =====================================================
    // NOTIFICATION LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "REFUND_NOTIFICATION",
        module: "NOTIFICATION",
        entityType: "REFUND",
        description:
            $"Retry notification sent for refund {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    TempData["Success"] =
        "Refund retry initiated successfully.";

    return RedirectToAction("Refunds");
}

// =====================================================
// SAVE REFUND NOTES
// =====================================================

[HttpPost]
public async Task<IActionResult> SaveRefundNotes(
    long refundId,
    string notes)
{
    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == refundId);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }

    refund.admin_notes = notes;

    refund.updated_at =
        DateTime.UtcNow;

    await _context.SaveChangesAsync();

    await _activityLogger.LogAsync(
        action: "SAVE_REFUND_NOTES",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Admin notes updated for {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    TempData["Success"] =
        "Admin notes saved successfully.";

    return RedirectToAction(
        "RefundDetails",
        new { id = refundId });
}

// =====================================================
// EXPORT REFUNDS CSV
// =====================================================

public IActionResult ExportRefunds()
{
    var refunds = _context.VwRefundSummaries
        .AsNoTracking()
        .ToList();

    var builder =
        new System.Text.StringBuilder();

    // =====================================================
    // CSV HEADER
    // =====================================================

    builder.AppendLine(
        "RefundRef,BookingRef,TransactionRef,UserName,UserEmail,RefundAmount,RefundStatus,RefundMethod,RequestedAt");

    // =====================================================
    // CSV ROWS
    // =====================================================

    foreach (var item in refunds)
    {
        builder.AppendLine(
            $"{item.RefundRef}," +
            $"{item.BookingRef}," +
            $"{item.TransactionRef}," +
            $"{item.UserName}," +
            $"{item.UserEmail}," +
            $"{item.RefundAmount}," +
            $"{item.RefundStatus}," +
            $"{item.RefundMethod}," +
            $"{item.RequestedAt}"
        );
    }

    // =====================================================
    // DOWNLOAD CSV FILE
    // =====================================================

    return File(
        System.Text.Encoding.UTF8.GetBytes(
            builder.ToString()),
        "text/csv",
        $"refunds_{DateTime.Now:yyyyMMddHHmmss}.csv"
    );
}



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
    // LOAD USER ROLES
    // =====================================================

    var userRoles = _context.UserRoleMappings
        .Where(x =>
            x.UserId == id &&
            x.IsActive)
        .Join(
            _context.Roles,
            map => map.RoleId,
            role => role.Id,
            (map, role) => role.RoleName
        )
        .Distinct()
        .ToList();

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

    decimal totalSpent = transactions.Any()
    ? transactions
        .Where(x => x.TransactionStatus == "SUCCESS")
        .Sum(x => x.TransactionAmount ?? 0)
    : 0;

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
    // BUILD RECENT ACTIVITIES
    // =====================================================

    var recentActivities = new List<string>();

    foreach (var txn in transactions)
    {
        recentActivities.Add(

            $"Transaction {txn.TransactionStatus} | " +
            $"{CurrencyFormatter.FormatRupees(txn.TransactionAmount)} | " +
            $"{txn.PaymentMethod ?? "NA"} | " +
            $"{txn.BookingCreatedAt:dd MMM yyyy hh:mm tt}"

        );
    }

    if (user.UpdatedAt != null)
    {
        recentActivities.Add(

            $"Profile updated on " +
            $"{user.UpdatedAt:dd MMM yyyy hh:mm tt}"

        );
    }

    recentActivities.Add(

        $"Account registered on " +
        $"{user.CreatedAt:dd MMM yyyy hh:mm tt}"

    );

    // =====================================================
    // HUMAN COMMENT:
    // CREATE VIEW MODEL
    // =====================================================

    var model = new AdminUserDetailsViewModel
    {
        UserId = user.Id,

        Name =
            string.IsNullOrWhiteSpace(user.Name)
                ? "NA"
                : user.Name,

        Email =
            string.IsNullOrWhiteSpace(user.Email)
                ? "NA"
                : user.Email,

        Mobile =
            string.IsNullOrWhiteSpace(user.Mobile)
                ? "NA"
                : user.Mobile,

        Language =
            string.IsNullOrWhiteSpace(user.Language)
                ? "NA"
                : user.Language,

        Genre =
            string.IsNullOrWhiteSpace(user.Genre)
                ? "NA"
                : user.Genre,

        Country =
            string.IsNullOrWhiteSpace(user.Country)
                ? "NA"
                : user.Country,

        State =
            string.IsNullOrWhiteSpace(user.State)
                ? "NA"
                : user.State,

        District =
            string.IsNullOrWhiteSpace(user.District)
                ? "NA"
                : user.District,

        Address =
            string.IsNullOrWhiteSpace(user.Address)
                ? "NA"
                : user.Address,

        Pincode =
            string.IsNullOrWhiteSpace(user.Pincode)
                ? "NA"
                : user.Pincode,

        // =====================================================
        // HUMAN COMMENT:
        // PROFILE IMAGE
        // =====================================================

        ProfileImagePath =
            string.IsNullOrWhiteSpace(user.ProfileImagePath)
                ? "/images/default-user.png"
                : user.ProfileImagePath,

        IsActive = user.is_active,

        IsDeleted = user.is_deleted,

        RegisteredAt = user.CreatedAt,

        LastLoginAt =
    user.UpdatedAt ?? user.CreatedAt,

        // =====================================================
        // HUMAN COMMENT:
        // WALLET
        // =====================================================

        WalletBalance =
            wallet?.WalletBalance ?? 0,

        // =====================================================
        // HUMAN COMMENT:
        // TRANSACTION STATS
        // =====================================================

        TotalTransactions = transactions.Count,

        SuccessTransactions = successCount,

        FailedTransactions = failedCount,

        PendingTransactions = pendingCount,

        TotalSpent = totalSpent,

        LastTransactionRef =
            lastTransaction?.TransactionRef ?? "NA",

        LastTransactionStatus =
            lastTransaction?.TransactionStatus ?? "NA",

        LastTransactionDate =
            lastTransaction?.BookingCreatedAt,

        // =====================================================
        // HUMAN COMMENT:
        // LAST TRANSACTIONS
        // =====================================================

        LastTransactions = transactions,

        // =====================================================
        // HUMAN COMMENT:
        // ACTIVITIES
        // =====================================================

        RecentActivities = recentActivities,

        // =====================================================
        // HUMAN COMMENT:
        // USER ACCESS ROLES
        // =====================================================

        UserAccess = userRoles
    };

    return View(model);
}
// =====================================================
// HUMAN COMMENT:
// ADMIN USER ACCESS PAGE
// =====================================================

// =====================================================
// HUMAN COMMENT:
// ADMIN USER ACCESS PAGE
// =====================================================

public IActionResult UserAccess()
{
    // =====================================================
    // HUMAN COMMENT:
    // LOAD ACTIVE USERS
    // =====================================================

    var users = _context.Users
        .AsNoTracking()
        .Where(x => !x.is_deleted)
        .OrderBy(x => x.Name)
        .ToList();

    // =====================================================
    // HUMAN COMMENT:
    // LOAD ACTIVE USER ROLE MAPPINGS
    // =====================================================

    var mappings = _context.UserRoleMappings
        .AsNoTracking()
        .Where(x => x.IsActive)
        .ToList();

    // =====================================================
    // HUMAN COMMENT:
    // LOAD ALL ROLES
    // =====================================================

    var roles = _context.Roles
        .AsNoTracking()
        .ToList();

    // =====================================================
    // HUMAN COMMENT:
    // BUILD USER ROLE VIEW MODEL
    // =====================================================

    var model = users.Select(user => new AdminUserRoleViewModel
    {
        UserId = user.Id,

        UserName =
            string.IsNullOrWhiteSpace(user.Name)
                ? "NA"
                : user.Name,

        UserEmail =
            string.IsNullOrWhiteSpace(user.Email)
                ? "NA"
                : user.Email,

        IsActive = user.is_active,

        // =====================================================
        // HUMAN COMMENT:
        // CURRENT USER ROLE IDS
        // =====================================================

        RoleIds = mappings
            .Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId)
            .Distinct()
            .ToList(),

        // =====================================================
        // HUMAN COMMENT:
        // CURRENT USER ROLE NAMES
        // =====================================================

        Roles = mappings
            .Where(x => x.UserId == user.Id)
            .Join(
                roles,
                map => map.RoleId,
                role => role.Id,
                (map, role) => role.RoleName
            )
            .Distinct()
            .ToList()

    }).ToList();

    ViewBag.AllRoles = roles;

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
[HttpPost]
public IActionResult AddUserRole(UserRoleUpdateViewModel request)
{
    // =====================================================
    // HUMAN COMMENT:
    // VALIDATE USER EXISTS
    // =====================================================

    var userExists =
        _context.Users.Any(x =>
            x.Id == request.UserId);

    if (!userExists)
    {
        TempData["Error"] =
            "User not found.";

        return RedirectToAction("UserAccess");
    }

    // =====================================================
    // HUMAN COMMENT:
    // VALIDATE ROLE EXISTS
    // =====================================================

    var roleExists =
        _context.Roles.Any(x =>
            x.Id == request.RoleId);

    if (!roleExists)
    {
        TempData["Error"] =
            "Role not found.";

        return RedirectToAction("UserAccess");
    }

    // =====================================================
    // HUMAN COMMENT:
    // PREVENT DUPLICATE ROLE ACCESS
    // =====================================================

    bool alreadyExists =
        _context.UserRoleMappings.Any(x =>
            x.UserId == request.UserId &&
            x.RoleId == request.RoleId &&
            x.IsActive);

    if (alreadyExists)
    {
        TempData["Error"] =
            "User already has this role.";

        return RedirectToAction("UserAccess");
    }

    // =====================================================
    // HUMAN COMMENT:
    // ADD NEW ROLE ACCESS
    // =====================================================

    var mapping = new UserRoleMapping
    {
        UserId = request.UserId,

        RoleId = request.RoleId,

        AssignedAt = DateTime.UtcNow,

        IsActive = true
    };

    _context.UserRoleMappings.Add(mapping);

    _context.SaveChanges();

    TempData["Success"] =
        "Role assigned successfully.";

    return RedirectToAction("UserAccess");
}

[HttpPost]
public IActionResult RemoveUserRole(long userId, long roleId)
{
    // =====================================================
    // HUMAN COMMENT:
    // FIND ACTIVE ROLE MAPPING
    // =====================================================

    var mapping = _context.UserRoleMappings
        .FirstOrDefault(x =>
            x.UserId == userId &&
            x.RoleId == roleId &&
            x.IsActive);

    if (mapping == null)
    {
        TempData["Error"] =
            "Role mapping not found.";

        return RedirectToAction("UserAccess");
    }

    // =====================================================
    // HUMAN COMMENT:
    // DEACTIVATE ROLE ACCESS
    // =====================================================

    mapping.IsActive = false;

    _context.SaveChanges();

    TempData["Success"] =
        "Role removed successfully.";

    return RedirectToAction("UserAccess");
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
