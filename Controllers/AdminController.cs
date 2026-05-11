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
}