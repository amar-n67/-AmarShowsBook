using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.Admin;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using AmarShowsBook.Helpers;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Globalization;

namespace AmarShowsBook.Controllers
{
    // Admin pages all pass through OnActionExecutionAsync, so page access and button actions use the same RBAC map.
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;


        private readonly IActivityLogger _activityLogger;


        private readonly RbacService _rbacService;

        private readonly OtpDeliveryService _emailDeliveryService;



        public AdminController(
            ApplicationDbContext context,
            IActivityLogger activityLogger,
            RbacService rbacService,
            OtpDeliveryService emailDeliveryService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _rbacService = rbacService;
            _emailDeliveryService = emailDeliveryService;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var actionName = context.ActionDescriptor.RouteValues["action"] ?? string.Empty;
            var currentUserId = TryGetSessionUserId();

            if (RequiresAdminDashboardRole(actionName) &&
                (currentUserId == null || !_rbacService.CanOpenAdminDashboard(currentUserId.Value)))
            {
                TempData["Error"] = "Only Administrator, Super Admin, or Developer can access the admin dashboard.";
                context.Result = RedirectToAction("ShowTime", "Home");
                return;
            }

            if (RequiresSuperAdminAreaRole(actionName) &&
                (currentUserId == null || !_rbacService.CanAccessSuperAdminArea(currentUserId.Value)))
            {
                TempData["Error"] = "Only Super Admin or Developer can access this admin page.";
                context.Result = RedirectToAction("ShowTime", "Home");
                return;
            }

            if (actionName.StartsWith("Export", StringComparison.OrdinalIgnoreCase) &&
                (currentUserId == null || !_rbacService.IsSuperAdmin(currentUserId.Value)))
            {
                TempData["Error"] = "Only Super Admin can export data.";
                context.Result = RedirectToAction("ShowTime", "Home");
                return;
            }

            if (TryGetAdminPermission(actionName, out var moduleCode, out var actionType))
            {
                await EnsureRbacInfrastructure();

                if (IsDashboardOnlyAdmin(currentUserId, actionName))
                {
                    TempData["Error"] = "dum_Admin can access only the allowed admin dashboard pages, Developer Profile, and My Profile.";
                    context.Result = RedirectToAction(nameof(Dashboard));
                    return;
                }

                if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, moduleCode, actionType))
                {
                    TempData["Error"] = "You do not have permission to access this admin feature.";
                    context.Result = actionName == nameof(Dashboard)
                        ? RedirectToAction("ShowTime", "Home")
                        : RedirectToAction(nameof(Dashboard));
                    return;
                }
            }

            ViewData["AdminNotificationCount"] = await GetAdminNotificationCount();

            await next();
        }

        private int? TryGetSessionUserId()
        {
            return int.TryParse(HttpContext.Session.GetString("UserId"), out var userId)
                ? userId
                : null;
        }

        private static bool RequiresAdminDashboardRole(string actionName)
        {
            return actionName.Equals(nameof(Dashboard), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals("Index", StringComparison.OrdinalIgnoreCase);
        }

        private static bool RequiresSuperAdminAreaRole(string actionName)
        {
            return actionName.Equals(nameof(Roles), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(CreateRole), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(UpdateRole), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Permissions), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(CreatePermission), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(ToggleRolePermission), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(ManageShows), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(CreateManagedShow), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(UpdateManagedShow), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(DeleteManagedShow), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDashboardOnlyAdmin(int? userId, string actionName)
        {
            if (userId == null || IsDumAdminAllowedAction(actionName))
            {
                return false;
            }

            return _rbacService.HasAnyActiveRole(userId.Value, "DUM_ADMIN") &&
                !_rbacService.HasAnyActiveRole(
                    userId.Value,
                    "AMAR_SUPER_ADMIN",
                    "AMAR_ADMIN",
                    "AMAR_DEVELOPER");
        }

        private static bool IsDumAdminAllowedAction(string actionName)
        {
            return actionName.Equals(nameof(Dashboard), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals("Index", StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Users), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Bookings), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Security), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Transactions), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(TransactionDetails), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Refunds), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(RefundDetails), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(CouponUsage), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Wallets), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Notifications), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(ActivityLogs), StringComparison.OrdinalIgnoreCase) ||
                   actionName.Equals(nameof(Versions), StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Dashboard()
        {


            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out var dashboardUserId) ||
                !_rbacService.CanOpenAdminDashboard(dashboardUserId))
            {
                return RedirectToAction("ShowTime", "Home");
            }


            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var now = DateTime.UtcNow;

            await EnsureAdminReportingViews();

            // These counts come from reporting views, which keeps dashboard cards consistent with admin tables.
            var vm = new AdminDashboardViewModel
            {

                TotalBookings =
                    _context.VwBookingCompleteDetails.Count(),

                FailedBookings =
                    _context.VwBookingCompleteDetails
                        .Count(x => x.IsError == 1),

                TodayBookings =
                    _context.VwBookingCompleteDetails
                        .Count(x => x.BookedAt >= today && x.BookedAt < tomorrow),

                ConfirmedBookings =
                    _context.VwBookingCompleteDetails
                        .Count(x => x.BookingStatus == "CONFIRMED"),

                CancelledBookings =
                    _context.VwBookingCompleteDetails
                        .Count(x => x.BookingStatus == "CANCELLED"),

                TotalTickets =
                    _context.VwBookingCompleteDetails
                        .Sum(x => (int?)x.TotalTickets) ?? 0,

                GrossBookingAmount =
                    _context.VwBookingCompleteDetails
                        .Sum(x => (decimal?)x.TotalAmount) ?? 0,

                PayableBookingAmount =
                    _context.VwBookingCompleteDetails
                        .Sum(x => x.PayableAmount ?? (decimal?)x.TotalAmount) ?? 0,


                SuccessfulPayments =
                    _context.VwBookingTransactionSummaries
                        .Count(x => x.IsPaymentError == 0),

                FailedPayments =
                    _context.VwBookingTransactionSummaries
                        .Count(x => x.IsPaymentError == 1),

                SuccessfulPaymentAmount =
                    _context.VwBookingTransactionSummaries
                        .Where(x => x.IsPaymentError == 0)
                        .Sum(x => x.TransactionAmount) ?? 0,


                TotalRefunds =
                    _context.VwRefundSummaries.Count(),

                FailedRefunds =
                    _context.VwRefundSummaries
                        .Count(x => x.IsRefundError == 1),

                PendingRefunds =
                    _context.VwRefundSummaries
                        .Count(x => x.RefundStatus == "PENDING"),

                ApprovedRefunds =
                    _context.VwRefundSummaries
                        .Count(x => x.RefundStatus == "APPROVED"),

                RejectedRefunds =
                    _context.VwRefundSummaries
                        .Count(x => x.RefundStatus == "REJECTED"),

                RequestedRefundAmount =
                    _context.VwRefundSummaries
                        .Sum(x => x.RefundAmount) ?? 0,


                InvoiceFailures =
                    _context.VwInvoiceSummaries
                        .Count(x => x.IsInvoiceError == 1),


                NotificationFailures =
                    _context.VwNotificationCenters
                        .Count(x => x.IsError == 1),

                TotalNotifications =
                    _context.VwNotificationCenters.Count(),

                DeliveredNotifications =
                    _context.VwNotificationCenters
                        .Count(x => x.Status == "DELIVERED"),

                PendingNotifications =
                    _context.VwNotificationCenters
                        .Count(x => x.Status == "PENDING"),

                HighPriorityNotifications =
                    _context.VwNotificationCenters
                        .Count(x => x.Priority == "HIGH"),


                TicketValidationIssues =
                    _context.VwTicketValidationSummaries
                        .Count(x => x.IsSecurityIssue == 1),

                ValidatedTickets =
                    _context.VwTicketValidationSummaries
                        .Count(x => x.IsSecurityIssue == 0),


                TotalWalletBalance =
                    _context.VwWalletSummaries
                        .Sum(x => (decimal?)x.WalletBalance) ?? 0,

                TotalCredits =
                    _context.VwWalletSummaries
                        .Sum(x => (decimal?)x.TotalCredits) ?? 0,

                TotalDebits =
                    _context.VwWalletSummaries
                        .Sum(x => (decimal?)x.TotalDebits) ?? 0,

                BlockedWalletBalance =
                    _context.VwWalletSummaries
                        .Sum(x => (decimal?)x.BlockedBalance) ?? 0,


                TotalUsers =
                    _context.Users.Count(),

                TotalMovies =
                    _context.Movies.Count(),

                TotalStandups =
                    _context.StandupShows.Count(),

                TotalLiveStreams =
                    _context.LiveStreams.Count(),

                TotalSchedules =
                    _context.ShowSchedules.Count(),

                UpcomingSchedules =
                    _context.ShowSchedules
                        .Count(x => x.StartTime >= now),

                TodaySchedules =
                    _context.ShowSchedules
                        .Count(x => x.StartTime >= today && x.StartTime < tomorrow),

                TotalScreens =
                    _context.Screens.Count(),

                TotalVenues =
                    _context.Venues.Count(),

                ActiveRoles =
                    _context.Roles.Count(x => x.IsActive)
            };

            vm.BookingStatusBreakdown = await _context.VwBookingCompleteDetails
                .GroupBy(x => x.BookingStatus)
                .Select(x => new DashboardBreakdownItem
                {
                    Label = x.Key ?? "NA",
                    Count = x.Count(),
                    Amount = x.Sum(y => y.PayableAmount ?? (decimal?)y.TotalAmount) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            vm.ShowTypeBreakdown = await _context.VwBookingCompleteDetails
                .GroupBy(x => x.ShowType)
                .Select(x => new DashboardBreakdownItem
                {
                    Label = x.Key ?? "NA",
                    Count = x.Count(),
                    Amount = x.Sum(y => y.PayableAmount ?? (decimal?)y.TotalAmount) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            vm.PaymentMethodBreakdown = await _context.VwBookingTransactionSummaries
                .GroupBy(x => x.PaymentMethod)
                .Select(x => new DashboardBreakdownItem
                {
                    Label = x.Key ?? "NA",
                    Count = x.Count(),
                    Amount = x.Sum(y => y.TransactionAmount) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            var recentBookings = await _context.VwBookingCompleteDetails
                .OrderByDescending(x => x.BookedAt)
                .Take(5)
                .Select(x => new
                {
                    x.BookingRef,
                    x.ShowTitle,
                    x.UserName,
                    x.BookingStatus,
                    x.BookedAt
                })
                .ToListAsync();

            vm.RecentBookings = recentBookings
                .Select(x => new DashboardRecentItem
                {
                    Title = string.IsNullOrWhiteSpace(x.BookingRef) ? "Booking" : x.BookingRef,
                    Detail = $"{(string.IsNullOrWhiteSpace(x.ShowTitle) ? "Show" : x.ShowTitle)} - {(string.IsNullOrWhiteSpace(x.UserName) ? "NA" : x.UserName)}",
                    Status = string.IsNullOrWhiteSpace(x.BookingStatus) ? "NA" : x.BookingStatus,
                    Time = x.BookedAt
                })
                .ToList();

            var recentRefunds = await _context.VwRefundSummaries
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new
                {
                    x.RefundRef,
                    x.BookingRef,
                    x.UserName,
                    x.RefundReason,
                    x.RefundStatus,
                    x.CreatedAt
                })
                .ToListAsync();

            vm.RecentRefunds = recentRefunds
                .Select(x => new DashboardRecentItem
                {
                    Title = string.IsNullOrWhiteSpace(x.RefundRef)
                        ? (string.IsNullOrWhiteSpace(x.BookingRef) ? "Refund request" : x.BookingRef)
                        : x.RefundRef,
                    Detail = $"{(string.IsNullOrWhiteSpace(x.UserName) ? "NA" : x.UserName)} - {(string.IsNullOrWhiteSpace(x.RefundReason) ? "No reason" : x.RefundReason)}",
                    Status = string.IsNullOrWhiteSpace(x.RefundStatus) ? "NA" : x.RefundStatus,
                    Time = x.CreatedAt
                })
                .ToList();
            


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

        public async Task<IActionResult> ExportDashboard()
        {
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out var dashboardUserId) ||
                !_rbacService.CanOpenAdminDashboard(dashboardUserId))
            {
                return RedirectToAction("ShowTime", "Home");
            }

            await EnsureAdminReportingViews();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var now = DateTime.UtcNow;

            var summaryRows = new List<(string Section, string Metric, decimal Value)>
            {
                ("Bookings", "Total Bookings", _context.VwBookingCompleteDetails.Count()),
                ("Bookings", "Failed Bookings", _context.VwBookingCompleteDetails.Count(x => x.IsError == 1)),
                ("Bookings", "Today Bookings", _context.VwBookingCompleteDetails.Count(x => x.BookedAt >= today && x.BookedAt < tomorrow)),
                ("Bookings", "Confirmed Bookings", _context.VwBookingCompleteDetails.Count(x => x.BookingStatus == "CONFIRMED")),
                ("Bookings", "Cancelled Bookings", _context.VwBookingCompleteDetails.Count(x => x.BookingStatus == "CANCELLED")),
                ("Bookings", "Tickets Sold", _context.VwBookingCompleteDetails.Sum(x => (int?)x.TotalTickets) ?? 0),
                ("Bookings", "Gross Booking Value", _context.VwBookingCompleteDetails.Sum(x => (decimal?)x.TotalAmount) ?? 0),
                ("Bookings", "Payable Booking Value", _context.VwBookingCompleteDetails.Sum(x => x.PayableAmount ?? (decimal?)x.TotalAmount) ?? 0),
                ("Payments", "Successful Payments", _context.VwBookingTransactionSummaries.Count(x => x.IsPaymentError == 0)),
                ("Payments", "Failed Payments", _context.VwBookingTransactionSummaries.Count(x => x.IsPaymentError == 1)),
                ("Payments", "Successful Payment Amount", _context.VwBookingTransactionSummaries.Where(x => x.IsPaymentError == 0).Sum(x => x.TransactionAmount) ?? 0),
                ("Refunds", "Total Refunds", _context.VwRefundSummaries.Count()),
                ("Refunds", "Failed Refunds", _context.VwRefundSummaries.Count(x => x.IsRefundError == 1)),
                ("Refunds", "Pending Refunds", _context.VwRefundSummaries.Count(x => x.RefundStatus == "PENDING")),
                ("Refunds", "Approved Refunds", _context.VwRefundSummaries.Count(x => x.RefundStatus == "APPROVED" || x.RefundStatus == "SUCCESS")),
                ("Refunds", "Rejected Refunds", _context.VwRefundSummaries.Count(x => x.RefundStatus == "REJECTED")),
                ("Refunds", "Requested Refund Amount", _context.VwRefundSummaries.Sum(x => x.RefundAmount) ?? 0),
                ("Wallets", "Total Wallet Balance", _context.VwWalletSummaries.Sum(x => (decimal?)x.WalletBalance) ?? 0),
                ("Wallets", "Total Credits", _context.VwWalletSummaries.Sum(x => (decimal?)x.TotalCredits) ?? 0),
                ("Wallets", "Total Debits", _context.VwWalletSummaries.Sum(x => (decimal?)x.TotalDebits) ?? 0),
                ("Wallets", "Blocked Wallet Balance", _context.VwWalletSummaries.Sum(x => (decimal?)x.BlockedBalance) ?? 0),
                ("Notifications", "Total Notifications", _context.VwNotificationCenters.Count()),
                ("Notifications", "Delivered Notifications", _context.VwNotificationCenters.Count(x => x.Status == "DELIVERED")),
                ("Notifications", "Pending Notifications", _context.VwNotificationCenters.Count(x => x.Status == "PENDING")),
                ("Notifications", "High Priority Notifications", _context.VwNotificationCenters.Count(x => x.Priority == "HIGH")),
                ("Notifications", "Notification Failures", _context.VwNotificationCenters.Count(x => x.IsError == 1)),
                ("Security", "Validated Tickets", _context.VwTicketValidationSummaries.Count(x => x.IsSecurityIssue == 0)),
                ("Security", "Security Issues", _context.VwTicketValidationSummaries.Count(x => x.IsSecurityIssue == 1)),
                ("Operations", "Invoice Failures", _context.VwInvoiceSummaries.Count(x => x.IsInvoiceError == 1)),
                ("Operations", "Total Users", _context.Users.Count()),
                ("Operations", "Movies", _context.Movies.Count()),
                ("Operations", "Standups", _context.StandupShows.Count()),
                ("Operations", "Live Streams", _context.LiveStreams.Count()),
                ("Operations", "Total Schedules", _context.ShowSchedules.Count()),
                ("Operations", "Upcoming Schedules", _context.ShowSchedules.Count(x => x.StartTime >= now)),
                ("Operations", "Today Shows", _context.ShowSchedules.Count(x => x.StartTime >= today && x.StartTime < tomorrow)),
                ("Operations", "Screens", _context.Screens.Count()),
                ("Operations", "Venues", _context.Venues.Count()),
                ("Operations", "Active Roles", _context.Roles.Count(x => x.IsActive))
            };

            var bookingStatusRows = await _context.VwBookingCompleteDetails
                .GroupBy(x => x.BookingStatus)
                .Select(x => new DashboardBreakdownItem
                {
                    Label = x.Key ?? "NA",
                    Count = x.Count(),
                    Amount = x.Sum(y => y.PayableAmount ?? (decimal?)y.TotalAmount) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var showTypeRows = await _context.VwBookingCompleteDetails
                .GroupBy(x => x.ShowType)
                .Select(x => new DashboardBreakdownItem
                {
                    Label = x.Key ?? "NA",
                    Count = x.Count(),
                    Amount = x.Sum(y => y.PayableAmount ?? (decimal?)y.TotalAmount) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var paymentRows = await _context.VwBookingTransactionSummaries
                .GroupBy(x => x.PaymentMethod)
                .Select(x => new DashboardBreakdownItem
                {
                    Label = x.Key ?? "NA",
                    Count = x.Count(),
                    Amount = x.Sum(y => y.TransactionAmount) ?? 0
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var recentBookings = await _context.VwBookingCompleteDetails
                .OrderByDescending(x => x.BookedAt)
                .Take(50)
                .Select(x => new
                {
                    x.BookingRef,
                    x.ShowTitle,
                    x.UserName,
                    x.BookingStatus,
                    x.PaymentStatus,
                    x.TotalTickets,
                    Amount = x.PayableAmount ?? x.TotalAmount,
                    x.BookedAt
                })
                .ToListAsync();

            var recentRefunds = await _context.VwRefundSummaries
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .Select(x => new
                {
                    x.RefundRef,
                    x.BookingRef,
                    x.TransactionRef,
                    x.UserName,
                    x.UserEmail,
                    x.RefundAmount,
                    x.RefundStatus,
                    x.RefundMethod,
                    x.RefundReason,
                    x.FailureReason,
                    x.RequestedAt,
                    x.CreatedAt
                })
                .ToListAsync();

            var allBookings = await _context.VwBookingCompleteDetails
                .OrderByDescending(x => x.BookedAt)
                .Select(x => new
                {
                    x.BookingId,
                    x.BookingRef,
                    x.UserId,
                    x.UserName,
                    x.UserEmail,
                    x.ShowType,
                    x.ShowTitle,
                    x.LocationName,
                    x.StartTime,
                    x.TotalTickets,
                    x.SeatNumbers,
                    x.TotalAmount,
                    x.TaxAmount,
                    x.DiscountAmount,
                    x.PayableAmount,
                    x.BookingStatus,
                    x.PaymentStatus,
                    x.PaymentMethod,
                    x.GatewayName,
                    x.TransactionRef,
                    x.TransactionStatus,
                    x.BookedAt,
                    x.ConfirmedAt,
                    x.CancelledAt,
                    x.IsError
                })
                .ToListAsync();

            var allTransactions = await _context.VwBookingTransactionSummaries
                .OrderByDescending(x => x.BookingCreatedAt)
                .Select(x => new
                {
                    x.TransactionId,
                    x.TransactionRef,
                    x.BookingId,
                    x.BookingRef,
                    x.UserId,
                    x.UserName,
                    x.UserEmail,
                    x.ShowType,
                    x.ShowTitle,
                    x.TotalAmount,
                    x.TransactionAmount,
                    x.Currency,
                    x.PaymentMethod,
                    x.GatewayName,
                    x.TransactionStatus,
                    x.FailureReason,
                    x.CompletedAt,
                    x.BookingCreatedAt,
                    x.IsPaymentError
                })
                .ToListAsync();

            var allWallets = await _context.VwWalletSummaries
                .OrderByDescending(x => x.WalletBalance)
                .Select(x => new
                {
                    x.WalletId,
                    x.UserId,
                    x.UserName,
                    x.UserEmail,
                    x.WalletBalance,
                    x.BlockedBalance,
                    x.TotalCredits,
                    x.TotalDebits,
                    x.LoyaltyPoints,
                    x.TotalWalletTransactions,
                    x.WalletStatus,
                    x.SuspensionReason,
                    x.SuspendedAt,
                    x.SuspendedBy,
                    x.ReactivatedAt,
                    x.ReactivatedBy,
                    x.ReactivationReason,
                    x.LastTransactionAt
                })
                .ToListAsync();

            var allNotifications = await _context.VwNotificationCenters
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.NotificationId,
                    x.UserName,
                    x.UserEmail,
                    x.TemplateCode,
                    x.TemplateName,
                    x.NotificationType,
                    x.Title,
                    x.Message,
                    x.Status,
                    x.Priority,
                    x.SentAt,
                    x.DeliveredAt,
                    x.ReadAt,
                    x.RetryCount,
                    x.FailureReason,
                    x.CreatedAt,
                    x.IsError
                })
                .ToListAsync();

            var securityRows = await _context.VwTicketValidationSummaries
                .OrderByDescending(x => x.IsSecurityIssue)
                .ThenByDescending(x => x.LastScannedAt ?? x.ValidatedAt)
                .Select(x => new
                {
                    x.ValidationLogId,
                    x.TicketId,
                    x.TicketNumber,
                    x.BookingRef,
                    x.UserName,
                    x.UserEmail,
                    x.ValidationStatus,
                    x.ValidationResult,
                    x.GateName,
                    x.DeviceId,
                    x.ScannerUser,
                    x.ValidatedAt,
                    x.ValidationCount,
                    x.LastScannedAt,
                    x.IsSecurityIssue
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var summary = workbook.Worksheets.Add("Summary");
            AddDashboardHeader(summary, "Dashboard Export");
            summary.Cell(5, 1).Value = "Section";
            summary.Cell(5, 2).Value = "Metric";
            summary.Cell(5, 3).Value = "Value";
            StyleHeaderRow(summary.Range("A5:C5"));

            for (var i = 0; i < summaryRows.Count; i++)
            {
                var row = i + 6;
                summary.Cell(row, 1).Value = summaryRows[i].Section;
                summary.Cell(row, 2).Value = summaryRows[i].Metric;
                summary.Cell(row, 3).Value = summaryRows[i].Value;
            }

            summary.Column(3).Style.NumberFormat.Format = "#,##0.00";
            StyleUsedRange(summary);

            AddBreakdownSheet(workbook, "Booking Status", bookingStatusRows);
            AddBreakdownSheet(workbook, "Show Type", showTypeRows);
            AddBreakdownSheet(workbook, "Payment Methods", paymentRows);
            AddGraphicsSheet(workbook, summaryRows, bookingStatusRows, paymentRows);

            var bookingsSheet = workbook.Worksheets.Add("Recent Bookings");
            AddDashboardHeader(bookingsSheet, "Recent Bookings");
            bookingsSheet.Cell(5, 1).InsertTable(recentBookings);
            StyleUsedRange(bookingsSheet);

            var refundsSheet = workbook.Worksheets.Add("Recent Refunds");
            AddDashboardHeader(refundsSheet, "Recent Refunds");
            refundsSheet.Cell(5, 1).InsertTable(recentRefunds);
            StyleUsedRange(refundsSheet);

            AddDataSheet(workbook, "All Bookings", allBookings);
            AddDataSheet(workbook, "All Transactions", allTransactions);
            AddDataSheet(workbook, "All Wallets", allWallets);
            AddDataSheet(workbook, "All Notifications", allNotifications);
            AddDataSheet(workbook, "Ticket Security", securityRows);

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "brand", "showtime-logo-cropped.png");
            foreach (var worksheet in workbook.Worksheets)
            {
                AddLogoIfAvailable(worksheet, logoPath);
                worksheet.SheetView.FreezeRows(5);
                worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                worksheet.PageSetup.PagesWide = 1;
                worksheet.PageSetup.PagesTall = 0;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            await _activityLogger.LogAsync(
                action: "EXPORT_ADMIN_DASHBOARD",
                module: "ADMIN",
                entityType: "DASHBOARD",
                description: "Admin dashboard Excel export generated",
                status: "SUCCESS",
                isError: 0
            );

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"showtime_dashboard_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private static void AddDashboardHeader(IXLWorksheet sheet, string title)
        {
            sheet.Cell(1, 2).Value = "showTime";
            sheet.Cell(1, 2).Style.Font.FontSize = 22;
            sheet.Cell(1, 2).Style.Font.Bold = true;
            sheet.Cell(1, 2).Style.Font.FontColor = XLColor.FromHtml("#111827");

            sheet.Cell(2, 2).Value = title;
            sheet.Cell(2, 2).Style.Font.FontSize = 14;
            sheet.Cell(2, 2).Style.Font.Bold = true;
            sheet.Cell(2, 2).Style.Font.FontColor = XLColor.FromHtml("#475569");

            sheet.Cell(3, 2).Value = $"Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}";
            sheet.Cell(3, 2).Style.Font.FontColor = XLColor.FromHtml("#64748b");
        }

        private static void AddLogoIfAvailable(IXLWorksheet sheet, string logoPath)
        {
            if (!System.IO.File.Exists(logoPath))
            {
                return;
            }

            sheet.AddPicture(logoPath)
                .MoveTo(sheet.Cell(1, 1))
                .WithSize(54, 54);
        }

        private static void StyleHeaderRow(IXLRange range)
        {
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#111827");
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Font.Bold = true;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void StyleUsedRange(IXLWorksheet sheet)
        {
            var range = sheet.RangeUsed();
            if (range == null)
            {
                return;
            }

            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = XLColor.FromHtml("#CBD5E1");
            range.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Alignment.WrapText = true;

            sheet.Columns().AdjustToContents(8, 80);
            sheet.Rows().AdjustToContents();
        }

        private static void AddBreakdownSheet(
            XLWorkbook workbook,
            string sheetName,
            List<DashboardBreakdownItem> rows)
        {
            var sheet = workbook.Worksheets.Add(sheetName);
            AddDashboardHeader(sheet, sheetName);
            sheet.Cell(5, 1).Value = "Label";
            sheet.Cell(5, 2).Value = "Count";
            sheet.Cell(5, 3).Value = "Amount";
            StyleHeaderRow(sheet.Range("A5:C5"));

            for (var i = 0; i < rows.Count; i++)
            {
                var row = i + 6;
                sheet.Cell(row, 1).Value = rows[i].Label;
                sheet.Cell(row, 2).Value = rows[i].Count;
                sheet.Cell(row, 3).Value = rows[i].Amount;
            }

            sheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
            StyleUsedRange(sheet);
        }

        private static void AddDataSheet<T>(
            XLWorkbook workbook,
            string sheetName,
            IEnumerable<T> rows)
        {
            var sheet = workbook.Worksheets.Add(sheetName);
            AddDashboardHeader(sheet, sheetName);
            sheet.Cell(5, 1).InsertTable(rows);
            StyleUsedRange(sheet);
        }

        private static void AddGraphicsSheet(
            XLWorkbook workbook,
            List<(string Section, string Metric, decimal Value)> summaryRows,
            List<DashboardBreakdownItem> bookingStatusRows,
            List<DashboardBreakdownItem> paymentRows)
        {
            var sheet = workbook.Worksheets.Add("Data Graphics");
            AddDashboardHeader(sheet, "Graphical Summary");

            sheet.Cell(5, 1).Value = "Dashboard Metrics";
            sheet.Range("A5:D5").Merge();
            sheet.Range("A5:D5").Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            sheet.Range("A5:D5").Style.Font.FontColor = XLColor.White;
            sheet.Range("A5:D5").Style.Font.Bold = true;

            var chartRows = summaryRows
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .Take(16)
                .ToList();
            var maxValue = chartRows.Any() ? chartRows.Max(x => x.Value) : 1m;

            for (var i = 0; i < chartRows.Count; i++)
            {
                var row = i + 6;
                var barWidth = Math.Max(1, (int)Math.Round(chartRows[i].Value / maxValue * 42m));
                sheet.Cell(row, 1).Value = chartRows[i].Metric;
                sheet.Cell(row, 2).Value = chartRows[i].Value;
                sheet.Cell(row, 3).Value = new string('|', barWidth);
                sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#0F766E");
                sheet.Cell(row, 3).Style.Font.Bold = true;
                sheet.Cell(row, 4).Value = chartRows[i].Section;
            }

            var bookingStart = chartRows.Count + 9;
            sheet.Cell(bookingStart, 1).Value = "Booking Status";
            sheet.Range(bookingStart, 1, bookingStart, 4).Merge();
            sheet.Range(bookingStart, 1, bookingStart, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9A441");
            sheet.Range(bookingStart, 1, bookingStart, 4).Style.Font.Bold = true;

            AddSmallBars(sheet, bookingStatusRows, bookingStart + 1, "#B91C1C");

            var paymentStart = bookingStart + bookingStatusRows.Count + 4;
            sheet.Cell(paymentStart, 1).Value = "Payment Methods";
            sheet.Range(paymentStart, 1, paymentStart, 4).Merge();
            sheet.Range(paymentStart, 1, paymentStart, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#14B8A6");
            sheet.Range(paymentStart, 1, paymentStart, 4).Style.Font.Bold = true;

            AddSmallBars(sheet, paymentRows, paymentStart + 1, "#D97706");

            sheet.Column(2).Style.NumberFormat.Format = "#,##0.00";
            StyleUsedRange(sheet);
        }

        private static void AddSmallBars(
            IXLWorksheet sheet,
            List<DashboardBreakdownItem> rows,
            int startRow,
            string color)
        {
            var maxCount = rows.Any() ? rows.Max(x => x.Count) : 1;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = startRow + i;
                var barWidth = Math.Max(1, (int)Math.Round(rows[i].Count / (double)maxCount * 36));
                sheet.Cell(row, 1).Value = rows[i].Label;
                sheet.Cell(row, 2).Value = rows[i].Count;
                sheet.Cell(row, 3).Value = new string('|', barWidth);
                sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml(color);
                sheet.Cell(row, 3).Style.Font.Bold = true;
                sheet.Cell(row, 4).Value = rows[i].Amount;
            }
        }



        public IActionResult Roles()
        {
            var roles = _context.Roles
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList();

            return View(roles);
        }


        public async Task<IActionResult> Security(int page = 1)
        {
            // Security joins scanner devices with ticket validation logs for gate-entry investigation.
            const int pageSize = 50;

            await EnsureSecurityInfrastructure();

            page = Math.Max(1, page);

            var query = _context.VwTicketValidationSummaries
                .AsNoTracking()
                .OrderByDescending(x => x.IsSecurityIssue)
                .ThenByDescending(x => x.LastScannedAt ?? x.ValidatedAt)
                .ThenByDescending(x => x.ValidationLogId);

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalCount;
            ViewBag.SecurityIssueCount = await _context.VwTicketValidationSummaries
                .CountAsync(x => x.IsSecurityIssue == 1);
            ViewBag.ValidatedCount = await _context.VwTicketValidationSummaries
                .CountAsync(x => x.IsSecurityIssue == 0);
            ViewBag.ActiveDeviceCount = await _context.ScannerDevices
                .CountAsync(x => x.DeviceStatus == "ACTIVE");
            ViewBag.ScannerDevices = await _context.ScannerDevices
                .AsNoTracking()
                .OrderByDescending(x => x.LastActiveAt ?? x.CreatedAt)
                .Take(8)
                .ToListAsync();

            return View("~/Views/Admin/Security/Index.cshtml", rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcknowledgeSecurityAlerts()
        {
            await _activityLogger.LogAsync(
                action: "ACKNOWLEDGE_SECURITY_ALERTS",
                module: "SCANNER",
                entityType: "TICKET_VALIDATION",
                description: "Admin reviewed ticket security alerts",
                status: "SUCCESS",
                isError: 0
            );

            TempData["Success"] = "Security alerts reviewed.";

            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSecurityValidation(
            string ticketNumber,
            string validationStatus,
            string validationResult,
            string? gateName,
            string? deviceId,
            string? remarks)
        {
            await EnsureSecurityInfrastructure();

            ticketNumber = (ticketNumber ?? string.Empty).Trim();
            validationStatus = NormalizeSecurityStatus(validationStatus);
            validationResult = NormalizeSecurityResult(validationResult);
            gateName = string.IsNullOrWhiteSpace(gateName) ? "Admin Gate" : gateName.Trim();
            deviceId = string.IsNullOrWhiteSpace(deviceId) ? "ADMIN-CONSOLE" : deviceId.Trim();

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(x => x.TicketNumber == ticketNumber);

            if (ticket == null)
            {
                TempData["Error"] = "Ticket number not found.";
                return RedirectToAction(nameof(Security));
            }

            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ticket.BookingId);

            var now = DateTime.Now;

            _context.TicketValidationLogs.Add(new TicketValidationLog
            {
                TicketId = ticket.Id,
                BookingId = ticket.BookingId,
                UserId = booking?.UserId,
                ScannedQrToken = ticket.QrToken,
                ValidationStatus = validationStatus,
                ValidationResult = validationResult,
                GateName = gateName,
                DeviceId = deviceId,
                ScannerUser = HttpContext.Session.GetString("UserName") ?? "Admin",
                ScannerIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Remarks = string.IsNullOrWhiteSpace(remarks) ? "Manual admin validation" : remarks.Trim(),
                Metadata = "{}",
                ScannedAt = now
            });

            ticket.ValidationStatus = validationStatus == "BLOCKED" ? "BLOCKED" : "SCANNED";
            ticket.ValidationCount += 1;
            ticket.LastScannedAt = now;
            ticket.LastScannedGate = gateName;
            ticket.UpdatedAt = now;

            await TouchScannerDevice(deviceId, gateName, now);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "ADD_TICKET_VALIDATION",
                module: "SCANNER",
                entityType: "TICKET",
                entityId: ticket.Id > int.MaxValue ? null : (int)ticket.Id,
                description: $"Admin added {validationResult} validation for ticket {ticket.TicketNumber}",
                status: validationResult == "SUCCESS" ? "SUCCESS" : "WARNING",
                isError: validationResult == "SUCCESS" ? 0 : 1
            );

            TempData["Success"] = "Ticket validation record added.";

            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearSecurityAlert(long validationLogId)
        {
            await EnsureSecurityInfrastructure();

            var log = await _context.TicketValidationLogs
                .FirstOrDefaultAsync(x => x.Id == validationLogId);

            if (log == null)
            {
                TempData["Error"] = "Security alert log not found.";
                return RedirectToAction(nameof(Security));
            }

            log.ValidationStatus = "SCANNED";
            log.ValidationResult = "SUCCESS";
            log.Remarks = AppendAdminRemark(log.Remarks, "Alert cleared by admin");

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(x => x.Id == log.TicketId);

            if (ticket != null)
            {
                ticket.ValidationStatus = "SCANNED";
                ticket.LastScannedAt = DateTime.Now;
                ticket.LastScannedGate = log.GateName;
                ticket.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "CLEAR_SECURITY_ALERT",
                module: "SCANNER",
                entityType: "TICKET_VALIDATION",
                entityId: validationLogId > int.MaxValue ? null : (int)validationLogId,
                description: $"Admin cleared security alert log {validationLogId}",
                status: "SUCCESS",
                isError: 0
            );

            TempData["Success"] = "Security alert cleared.";

            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockTicketFromSecurity(long ticketId)
        {
            await EnsureSecurityInfrastructure();

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(x => x.Id == ticketId);

            if (ticket == null)
            {
                TempData["Error"] = "Ticket not found.";
                return RedirectToAction(nameof(Security));
            }

            var booking = await _context.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ticket.BookingId);

            var now = DateTime.Now;

            ticket.ValidationStatus = "BLOCKED";
            ticket.LastScannedAt = now;
            ticket.LastScannedGate = "Admin Security";
            ticket.UpdatedAt = now;

            _context.TicketValidationLogs.Add(new TicketValidationLog
            {
                TicketId = ticket.Id,
                BookingId = ticket.BookingId,
                UserId = booking?.UserId,
                ScannedQrToken = ticket.QrToken,
                ValidationStatus = "BLOCKED",
                ValidationResult = "ADMIN_BLOCKED",
                GateName = "Admin Security",
                DeviceId = "ADMIN-CONSOLE",
                ScannerUser = HttpContext.Session.GetString("UserName") ?? "Admin",
                ScannerIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Remarks = "Ticket blocked by admin from Security page",
                Metadata = "{}",
                ScannedAt = now
            });

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "BLOCK_TICKET_SECURITY",
                module: "SCANNER",
                entityType: "TICKET",
                entityId: ticket.Id > int.MaxValue ? null : (int)ticket.Id,
                description: $"Admin blocked ticket {ticket.TicketNumber} from scanner validation",
                status: "WARNING",
                isError: 1
            );

            TempData["Success"] = "Ticket blocked from scanner validation.";

            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterScannerDevice(
            string deviceCode,
            string? deviceName,
            string? gateName)
        {
            await EnsureSecurityInfrastructure();

            deviceCode = (deviceCode ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(deviceCode))
            {
                TempData["Error"] = "Device code is required.";
                return RedirectToAction(nameof(Security));
            }

            var now = DateTime.Now;
            var device = await _context.ScannerDevices
                .FirstOrDefaultAsync(x => x.DeviceCode == deviceCode);

            if (device == null)
            {
                _context.ScannerDevices.Add(new ScannerDevice
                {
                    DeviceCode = deviceCode,
                    DeviceName = string.IsNullOrWhiteSpace(deviceName) ? deviceCode : deviceName.Trim(),
                    GateName = string.IsNullOrWhiteSpace(gateName) ? "Main Gate" : gateName.Trim(),
                    DeviceStatus = "ACTIVE",
                    LastActiveAt = now,
                    CreatedAt = now
                });
            }
            else
            {
                device.DeviceName = string.IsNullOrWhiteSpace(deviceName) ? device.DeviceName : deviceName.Trim();
                device.GateName = string.IsNullOrWhiteSpace(gateName) ? device.GateName : gateName.Trim();
                device.DeviceStatus = "ACTIVE";
                device.LastActiveAt = now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Scanner device saved.";

            return RedirectToAction(nameof(Security));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleCode, string roleName, string? roleDescription)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "ROLE", "CREATE"))
            {
                TempData["Error"] = "You do not have permission to create roles.";
                return RedirectToAction(nameof(Roles));
            }

            TempData["Error"] = "Custom roles are disabled. Use only Super Admin, Administrator, Developer, and User.";
            return RedirectToAction(nameof(Roles));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(long id, string roleName, string? roleDescription, bool isActive)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "ROLE", "UPDATE"))
            {
                TempData["Error"] = "You do not have permission to edit roles.";
                return RedirectToAction(nameof(Roles));
            }

            var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToAction(nameof(Roles));
            }

            var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AMAR_SUPER_ADMIN",
                "AMAR_ADMIN",
                "AMAR_DEVELOPER",
                "AMAR_USER",
                "DUM_ADMIN"
            };

            if (!allowedRoles.Contains(role.RoleCode))
            {
            TempData["Error"] = "Only the system roles can be edited.";
            return RedirectToAction(nameof(Roles));
        }

            role.RoleName = (roleName ?? role.RoleName).Trim();
            role.RoleDescription = roleDescription?.Trim();
            role.IsActive = role.RoleCode == "AMAR_SUPER_ADMIN" || isActive;
            role.IsSystemRole = true;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = HttpContext.Session.GetString("UserName") ?? "Admin";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Role updated successfully.";
            return RedirectToAction(nameof(Roles));
        }



      public IActionResult Permissions()
        {
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePermission(
            long moduleId,
            string permissionCode,
            string permissionName,
            string actionType,
            string? description)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "PERMISSION", "CREATE"))
            {
                TempData["Error"] = "You do not have permission to create permissions.";
                return RedirectToAction(nameof(Permissions));
            }

            permissionCode = NormalizeCode(permissionCode);
            actionType = NormalizeCode(actionType);

            if (moduleId <= 0 || string.IsNullOrWhiteSpace(permissionCode) || string.IsNullOrWhiteSpace(permissionName))
            {
                TempData["Error"] = "Module, permission code and permission name are required.";
                return RedirectToAction(nameof(Permissions));
            }

            if (await _context.Permissions.AnyAsync(x => x.PermissionCode == permissionCode))
            {
                TempData["Error"] = "Permission code already exists.";
                return RedirectToAction(nameof(Permissions));
            }

            _context.Permissions.Add(new Permission
            {
                ModuleId = moduleId,
                PermissionCode = permissionCode,
                PermissionName = permissionName.Trim(),
                ActionType = actionType,
                Description = description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Permission created successfully.";
            return RedirectToAction(nameof(Permissions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRolePermission(long roleId, long permissionId, bool grant)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "PERMISSION", "ASSIGN"))
            {
                TempData["Error"] = "You do not have permission to assign permissions.";
                return RedirectToAction(nameof(Permissions));
            }

            var mapping = await _context.RolePermissions
                .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);

            if (grant && mapping == null)
            {
                long? grantedBy = null;
                if (long.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId))
                {
                    grantedBy = currentUserId;
                }

                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    GrantedBy = grantedBy,
                    GrantedAt = DateTime.UtcNow
                });
            }
            else if (!grant && mapping != null)
            {
                _context.RolePermissions.Remove(mapping);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = grant ? "Permission granted." : "Permission removed.";
            return RedirectToAction(nameof(Permissions));
        }

        public async Task<IActionResult> ManageShows()
        {
            // This is the admin entry point for content, schedules, venues, screens, and generated seats.
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "SHOW", "VIEW"))
            {
                return RedirectToAction("Dashboard");
            }

            await EnsureAdminShowInfrastructure();

            var model = new ManageShowsViewModel
            {
                Schedules = await _context.ShowSchedules
                    .Include(x => x.Movie)
                    .Include(x => x.StandupShow)
                    .Include(x => x.LiveStream)
                    .Include(x => x.Location)
                    .Include(x => x.Screen)
                    .OrderByDescending(x => x.StartTime)
                    .Take(100)
                    .ToListAsync(),
                Locations = await _context.Locations.AsNoTracking().OrderBy(x => x.State).ThenBy(x => x.Area).ToListAsync(),
                Venues = await _context.Venues.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.VenueName).ToListAsync(),
                Screens = await _context.Screens.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.VenueId).ThenBy(x => x.ScreenName).ToListAsync()
            };

            var scheduleIds = model.Schedules.Select(x => x.Id).ToList();
            ViewBag.ScheduleSeatCounts = await _context.ScreenSeats
                .AsNoTracking()
                .Where(x => scheduleIds.Contains(x.ScheduleId))
                .GroupBy(x => x.ScheduleId)
                .Select(x => new { ScheduleId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.ScheduleId, x => x.Count);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManagedShow(ManageShowCreateViewModel request)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "SHOW", "CREATE"))
            {
                TempData["Error"] = "You do not have permission to create shows.";
                return RedirectToAction(nameof(ManageShows));
            }

            if (string.IsNullOrWhiteSpace(request.Title) || request.LocationId <= 0 || request.ScreenId <= 0)
            {
                TempData["Error"] = "Show title, location and screen are required.";
                return RedirectToAction(nameof(ManageShows));
            }

            await EnsureAdminShowInfrastructure();
            var resolvedScreenId = await ResolveManagedScreenId(request.VenueId, request.ScreenId);
            if (resolvedScreenId <= 0)
            {
                TempData["Error"] = "Please select a valid theater and screen.";
                return RedirectToAction(nameof(ManageShows));
            }
            request.ScreenId = resolvedScreenId;

            var duration = Math.Clamp(request.Duration, 15, 600);
            var type = request.Type switch
            {
                "Standup" => "Standup",
                "Live" => "Live",
                _ => "Movie"
            };

            var startTimes = BuildManagedShowStartTimes(request);
            var created = 0;
            var metadata = NormalizeManagedShowMetadata(
                request.SecondaryName,
                request.Cast,
                request.Description,
                request.PosterUrl,
                request.Images,
                request.TrailerUrl,
                request.ImdbRating);

            if (type == "Movie")
            {
                var movie = new Movie
                {
                    Title = request.Title.Trim(),
                    Director = metadata.SecondaryName,
                    Producer = metadata.SecondaryName ?? "Admin",
                    Cast = metadata.Cast ?? "TBA",
                    Duration = duration,
                    Description = metadata.Description,
                    PosterUrl = metadata.PosterUrl,
                    Images = metadata.Images,
                    TrailerUrl = metadata.TrailerUrl,
                    ImdbRating = metadata.ImdbRating
                };
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                foreach (var startTime in startTimes)
                {
                    var schedule = CreateSchedule(request, type, duration, startTime);
                    schedule.MovieId = movie.Id;
                    _context.ShowSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                    await GenerateManagedSeats(schedule.Id, request.ScreenId, request.TotalSeats, request.SilverPrice, request.GoldPrice, request.PremiumPrice);
                    created++;
                }
            }
            else if (type == "Standup")
            {
                var show = new StandupShow
                {
                    Title = request.Title.Trim(),
                    Comedian = metadata.SecondaryName ?? "TBA",
                    Duration = duration,
                    Description = metadata.Description,
                    PosterUrl = metadata.PosterUrl,
                    Images = metadata.Images,
                    TrailerUrl = metadata.TrailerUrl
                };
                _context.StandupShows.Add(show);
                await _context.SaveChangesAsync();

                foreach (var startTime in startTimes)
                {
                    var schedule = CreateSchedule(request, type, duration, startTime);
                    schedule.StandupShowId = show.Id;
                    _context.ShowSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                    await GenerateManagedSeats(schedule.Id, request.ScreenId, request.TotalSeats, request.SilverPrice, request.GoldPrice, request.PremiumPrice);
                    created++;
                }
            }
            else
            {
                var live = new LiveStream
                {
                    Title = request.Title.Trim(),
                    Host = metadata.SecondaryName ?? "TBA",
                    Duration = duration,
                    Description = metadata.Description,
                    PosterUrl = metadata.PosterUrl,
                    Images = metadata.Images,
                    TrailerUrl = metadata.TrailerUrl
                };
                _context.LiveStreams.Add(live);
                await _context.SaveChangesAsync();

                foreach (var startTime in startTimes)
                {
                    var schedule = CreateSchedule(request, type, duration, startTime);
                    schedule.LiveStreamId = live.Id;
                    _context.ShowSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                    await GenerateManagedSeats(schedule.Id, request.ScreenId, request.TotalSeats, request.SilverPrice, request.GoldPrice, request.PremiumPrice);
                    created++;
                }
            }

            TempData["Success"] = $"{created} show time(s) scheduled successfully.";
            return RedirectToAction(nameof(ManageShows));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateManagedShow(ManageShowUpdateViewModel request)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "SHOW", "UPDATE"))
            {
                TempData["Error"] = "You do not have permission to edit shows.";
                return RedirectToAction(nameof(ManageShows));
            }

            if (request.Id <= 0 || string.IsNullOrWhiteSpace(request.Title) || request.LocationId <= 0 || request.ScreenId <= 0)
            {
                TempData["Error"] = "Show title, location and screen are required.";
                return RedirectToAction(nameof(ManageShows));
            }

            await EnsureAdminShowInfrastructure();

            var schedule = await _context.ShowSchedules
                .Include(x => x.Movie)
                .Include(x => x.StandupShow)
                .Include(x => x.LiveStream)
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (schedule == null)
            {
                TempData["Error"] = "Show schedule not found.";
                return RedirectToAction(nameof(ManageShows));
            }

            var resolvedScreenId = await ResolveManagedScreenId(request.VenueId, request.ScreenId);
            if (resolvedScreenId <= 0)
            {
                TempData["Error"] = "Please select a valid theater and screen.";
                return RedirectToAction(nameof(ManageShows));
            }

            var duration = Math.Clamp(request.Duration, 15, 600);
            var startTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Local).ToUniversalTime();
            var metadata = NormalizeManagedShowMetadata(
                request.SecondaryName,
                request.Cast,
                request.Description,
                request.PosterUrl,
                request.Images,
                request.TrailerUrl,
                request.ImdbRating);

            schedule.LocationId = request.LocationId;
            schedule.ScreenId = resolvedScreenId;
            schedule.StartTime = startTime;
            schedule.EndTime = startTime.AddMinutes(duration);
            schedule.ShowDay = GetScheduleDayName(startTime);

            if (schedule.Movie != null)
            {
                schedule.Movie.Title = request.Title.Trim();
                schedule.Movie.Director = metadata.SecondaryName;
                schedule.Movie.Producer = metadata.SecondaryName ?? schedule.Movie.Producer ?? "Admin";
                schedule.Movie.Cast = metadata.Cast ?? "TBA";
                schedule.Movie.Duration = duration;
                schedule.Movie.Description = metadata.Description;
                schedule.Movie.PosterUrl = metadata.PosterUrl;
                schedule.Movie.Images = metadata.Images;
                schedule.Movie.TrailerUrl = metadata.TrailerUrl;
                schedule.Movie.ImdbRating = metadata.ImdbRating;
            }
            else if (schedule.StandupShow != null)
            {
                schedule.StandupShow.Title = request.Title.Trim();
                schedule.StandupShow.Comedian = metadata.SecondaryName ?? "TBA";
                schedule.StandupShow.Duration = duration;
                schedule.StandupShow.Description = metadata.Description;
                schedule.StandupShow.PosterUrl = metadata.PosterUrl;
                schedule.StandupShow.Images = metadata.Images;
                schedule.StandupShow.TrailerUrl = metadata.TrailerUrl;
            }
            else if (schedule.LiveStream != null)
            {
                schedule.LiveStream.Title = request.Title.Trim();
                schedule.LiveStream.Host = metadata.SecondaryName ?? "TBA";
                schedule.LiveStream.Duration = duration;
                schedule.LiveStream.Description = metadata.Description;
                schedule.LiveStream.PosterUrl = metadata.PosterUrl;
                schedule.LiveStream.Images = metadata.Images;
                schedule.LiveStream.TrailerUrl = metadata.TrailerUrl;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Show updated successfully.";
            return RedirectToAction(nameof(ManageShows));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteManagedShow(int id)
        {
            if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, "SHOW", "DELETE"))
            {
                TempData["Error"] = "You do not have permission to delete shows.";
                return RedirectToAction(nameof(ManageShows));
            }

            var schedule = await _context.ShowSchedules.FirstOrDefaultAsync(x => x.Id == id);
            if (schedule == null)
            {
                TempData["Error"] = "Show schedule not found.";
                return RedirectToAction(nameof(ManageShows));
            }

            var hasBookings = await _context.Bookings.AnyAsync(x => x.ScheduleId == id && x.BookingStatus != "CANCELLED");
            if (hasBookings)
            {
                TempData["Error"] = "This show has active bookings and cannot be deleted.";
                return RedirectToAction(nameof(ManageShows));
            }

            var locks = await _context.SeatLocks.Where(x => x.ScheduleId == id).ToListAsync();
            var seats = await _context.ScreenSeats.Where(x => x.ScheduleId == id).ToListAsync();
            _context.SeatLocks.RemoveRange(locks);
            _context.ScreenSeats.RemoveRange(seats);
            _context.ShowSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Show deleted successfully.";
            return RedirectToAction(nameof(ManageShows));
        }

        public IActionResult Users(int page = 1)
        {

            const int pageSize = 50;
            page = Math.Max(page, 1);

            var query = _context.Users
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var users = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalCount;

            return View(users);
        }


        [HttpPost]
        public async Task<IActionResult> DisableUser(int id)
        {

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


        public async Task<IActionResult> Bookings(int page = 1)
        {

            const int pageSize = 50;
            page = Math.Max(page, 1);

            await EnsureAdminReportingViews();

            var query =
                _context.VwBookingCompleteDetails
                    .AsNoTracking()
                    .OrderByDescending(x => x.BookedAt);

            var totalCount = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var bookings = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalCount;

            return View(bookings);
        }


        public async Task<IActionResult> Transactions(int page = 1)
        {

            const int pageSize = 50;

            page = Math.Max(page, 1);

            await EnsureAdminReportingViews();

            var query = _context.VwBookingTransactionSummaries
                .AsNoTracking()
                .OrderByDescending(x => x.BookingCreatedAt);

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
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalCount;
            ViewBag.SuccessCount = summary?.Success ?? 0;
            ViewBag.FailedCount = summary?.Failed ?? 0;

            return View(transactions);
        }


        public async Task<IActionResult> TransactionDetails(int id)
        {

            var transaction = await _context.VwBookingTransactionSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TransactionId == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }


        public async Task<IActionResult> ActivityLogs(int page = 1)
        {

            const int pageSize = 50;
            page = Math.Max(page, 1);

            await EnsureAdminReportingViews();

            var query = _context
                .VwEnterpriseActivityLogs
                .AsNoTracking()
                .OrderByDescending(x => x.activity_time);

            var totalCount = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            var logs = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = totalCount;

            return View(logs);
        }


        public IActionResult AccessManagement()
        {

            var access =
                _context.VwUserAccessMatrices
                    .AsNoTracking()
                    .ToList();

            return View(access);
        }


        public IActionResult Menus()
        {

            var menus =
                _context.VwUserApplicationMenus
                    .AsNoTracking()
                    .ToList();

            return View(menus);
        }


        public async Task<IActionResult> Refunds(int page = 1)
        {
            try
            {
                const int pageSize = 50;
                page = Math.Max(page, 1);

                await EnsureAdminReportingViews();

                var query = _context.VwRefundSummaries
                    .AsNoTracking()
                    .OrderByDescending(x => x.RequestedAt ?? x.CreatedAt);

                var totalCount = await query.CountAsync();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                page = Math.Min(page, totalPages);

                var refunds = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalRecords = totalCount;

                return View(refunds);
            }
            catch
            {

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalRecords = 0;

                return View(new List<VwRefundSummary>());
            }
        }


        public async Task<IActionResult> Wallets(int page = 1)
        {
            try
            {
                await EnsureWalletAdminSchema();

                const int pageSize = 50;
                page = Math.Max(page, 1);

                var query = _context.VwWalletSummaries
                    .AsNoTracking()
                    .OrderByDescending(x => x.LastTransactionAt);

                var totalCount = await query.CountAsync();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                page = Math.Min(page, totalPages);

                var wallets = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var walletIds =
                    wallets.Select(x => x.WalletId).ToList();

                var walletHistory =
                    await _context.WalletStatusHistories
                        .AsNoTracking()
                        .Where(x => walletIds.Contains(x.WalletId))
                        .OrderByDescending(x => x.CreatedAt)
                        .ToListAsync();

                ViewBag.WalletStatusHistory =
                    walletHistory
                        .GroupBy(x => x.WalletId)
                        .ToDictionary(x => x.Key, x => x.ToList());

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalRecords = totalCount;

                return View(wallets);
            }
            catch
            {

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalRecords = 0;
                ViewBag.WalletStatusHistory =
                    new Dictionary<long, List<WalletStatusHistory>>();

                return View(new List<VwWalletSummary>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateWallet(long walletId, string reactivationReason)
        {
            await EnsureWalletAdminSchema();

            reactivationReason =
                (reactivationReason ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(reactivationReason))
            {
                TempData["Error"] = "Reactivation reason is mandatory.";
                return RedirectToAction(nameof(Wallets), null, null, $"wallet-{walletId}");
            }

            var wallet =
                await _context.VwWalletSummaries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WalletId == walletId);

            if (wallet == null)
            {
                TempData["Error"] = "Wallet record was not found.";
                return RedirectToAction(nameof(Wallets));
            }

            if (!string.Equals(wallet.WalletStatus, "SUSPENDED", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] = "Only suspended wallets need reactivation.";
                return RedirectToAction(nameof(Wallets));
            }

            var adminName =
                HttpContext.Session.GetString("UserName") ??
                HttpContext.Session.GetString("UserEmail") ??
                "Admin";

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE public.user_wallets
SET
    wallet_status = 'ACTIVE',
    reactivated_at = CURRENT_TIMESTAMP,
    reactivated_by = {adminName},
    reactivation_reason = {reactivationReason},
    updated_at = CURRENT_TIMESTAMP
WHERE id = {walletId};

INSERT INTO public.wallet_status_history
(
    wallet_id,
    user_id,
    previous_status,
    new_status,
    action_type,
    action_reason,
    action_by,
    wallet_balance,
    blocked_balance,
    created_at
)
VALUES
(
    {walletId},
    {wallet.UserId},
    {wallet.WalletStatus ?? "NA"},
    'ACTIVE',
    'REACTIVATE',
    {reactivationReason},
    {adminName},
    {wallet.WalletBalance},
    {wallet.BlockedBalance},
    CURRENT_TIMESTAMP
);
");

            await _activityLogger.LogAsync(
                action: "REACTIVATE_WALLET",
                module: "WALLET",
                entityType: "USER_WALLET",
                entityId: walletId > int.MaxValue ? null : (int)walletId,
                description: $"Admin reactivated wallet for {wallet.UserEmail ?? wallet.UserName ?? walletId.ToString(CultureInfo.InvariantCulture)}. Reason: {reactivationReason}",
                status: "SUCCESS",
                isError: 0
            );

            TempData["Success"] = "Wallet account reactivated.";
            return RedirectToAction(nameof(Wallets));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendWallet(long walletId, string suspensionReason)
        {
            await EnsureWalletAdminSchema();

            suspensionReason =
                (suspensionReason ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(suspensionReason))
            {
                TempData["Error"] = "Suspension reason is mandatory.";
                return RedirectToAction(nameof(Wallets), null, null, $"wallet-{walletId}");
            }

            var wallet =
                await _context.VwWalletSummaries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WalletId == walletId);

            if (wallet == null)
            {
                TempData["Error"] = "Wallet record was not found.";
                return RedirectToAction(nameof(Wallets));
            }

            if (string.Equals(wallet.WalletStatus, "SUSPENDED", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] = "Wallet is already suspended.";
                return RedirectToAction(nameof(Wallets), null, null, $"wallet-{walletId}");
            }

            var adminName =
                HttpContext.Session.GetString("UserName") ??
                HttpContext.Session.GetString("UserEmail") ??
                "Admin";

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE public.user_wallets
SET
    wallet_status = 'SUSPENDED',
    suspension_reason = {suspensionReason},
    suspended_at = CURRENT_TIMESTAMP,
    suspended_by = {adminName},
    updated_at = CURRENT_TIMESTAMP
WHERE id = {walletId};

INSERT INTO public.wallet_status_history
(
    wallet_id,
    user_id,
    previous_status,
    new_status,
    action_type,
    action_reason,
    action_by,
    wallet_balance,
    blocked_balance,
    created_at
)
VALUES
(
    {walletId},
    {wallet.UserId},
    {wallet.WalletStatus ?? "NA"},
    'SUSPENDED',
    'SUSPEND',
    {suspensionReason},
    {adminName},
    {wallet.WalletBalance},
    {wallet.BlockedBalance},
    CURRENT_TIMESTAMP
);
");

            await _activityLogger.LogAsync(
                action: "SUSPEND_WALLET",
                module: "WALLET",
                entityType: "USER_WALLET",
                entityId: walletId > int.MaxValue ? null : (int)walletId,
                description: $"Admin suspended wallet for {wallet.UserEmail ?? wallet.UserName ?? walletId.ToString(CultureInfo.InvariantCulture)}. Reason: {suspensionReason}",
                status: "WARNING",
                isError: 1
            );

            TempData["Success"] = "Wallet account suspended.";
            return RedirectToAction(nameof(Wallets), null, null, $"wallet-{walletId}");
        }

private async Task EnsureWalletAdminSchema()
{
    await _context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE public.user_wallets
ADD COLUMN IF NOT EXISTS suspension_reason text;

ALTER TABLE public.user_wallets
ADD COLUMN IF NOT EXISTS suspended_at timestamp without time zone;

ALTER TABLE public.user_wallets
ADD COLUMN IF NOT EXISTS suspended_by varchar(255);

ALTER TABLE public.user_wallets
ADD COLUMN IF NOT EXISTS reactivated_at timestamp without time zone;

ALTER TABLE public.user_wallets
ADD COLUMN IF NOT EXISTS reactivated_by varchar(255);

ALTER TABLE public.user_wallets
ADD COLUMN IF NOT EXISTS reactivation_reason text;

CREATE TABLE IF NOT EXISTS public.wallet_status_history
(
    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    wallet_id bigint NOT NULL,
    user_id bigint NOT NULL,
    previous_status varchar(50),
    new_status varchar(50) NOT NULL,
    action_type varchar(50) NOT NULL,
    action_reason text NOT NULL,
    action_by varchar(255),
    wallet_balance numeric(15,2) DEFAULT 0,
    blocked_balance numeric(15,2) DEFAULT 0,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_wallet_status_history_wallet
ON public.wallet_status_history(wallet_id);

CREATE INDEX IF NOT EXISTS idx_wallet_status_history_created
ON public.wallet_status_history(created_at);

UPDATE public.user_wallets
SET
    suspension_reason = COALESCE(NULLIF(suspension_reason, ''), 'Suspended by admin. Review account activity before reactivation.'),
    suspended_at = COALESCE(suspended_at, updated_at, last_transaction_at, created_at, CURRENT_TIMESTAMP),
    suspended_by = COALESCE(NULLIF(suspended_by, ''), 'Admin')
WHERE wallet_status = 'SUSPENDED';

DROP VIEW IF EXISTS public.vw_wallet_summary;

CREATE VIEW public.vw_wallet_summary AS
SELECT
    uw.id AS wallet_id,
    u.""Id"" AS user_id,
    u.""Name"" AS user_name,
    u.""Email"" AS user_email,
    uw.wallet_balance,
    uw.blocked_balance,
    uw.loyalty_points,
    uw.wallet_status,
    uw.suspension_reason,
    uw.suspended_at,
    uw.suspended_by,
    uw.reactivated_at,
    uw.reactivated_by,
    uw.reactivation_reason,
    uw.last_transaction_at,
    count(wt.id) FILTER
    (
        WHERE COALESCE(wt.is_deleted, false) = false
          AND COALESCE(wt.status, wt.transaction_status, 'SUCCESS') = 'SUCCESS'
    ) AS total_wallet_transactions,
    COALESCE(sum(wt.amount) FILTER
    (
        WHERE wt.entry_type = 'CREDIT'
          AND COALESCE(wt.is_deleted, false) = false
          AND COALESCE(wt.status, wt.transaction_status, 'SUCCESS') = 'SUCCESS'
    ), 0) AS total_credits,
    COALESCE(sum(wt.amount) FILTER
    (
        WHERE wt.entry_type = 'DEBIT'
          AND COALESCE(wt.is_deleted, false) = false
          AND COALESCE(wt.status, wt.transaction_status, 'SUCCESS') = 'SUCCESS'
    ), 0) AS total_debits
FROM public.user_wallets uw
JOIN public.""Users"" u ON uw.user_id = u.""Id""
LEFT JOIN public.wallet_transactions wt ON uw.id = wt.wallet_id
GROUP BY uw.id, u.""Id"";
");
}


        public async Task<IActionResult> Notifications(int page = 1)
        {
            await EnsureAdminReportingViews();

            var actionNotifications = await BuildAdminNotificationActions();
            var archivedActionNotifications = await BuildAdminNotificationArchiveActions();

            ViewBag.ActionNotifications = actionNotifications;
            ViewBag.ArchivedActionNotifications = archivedActionNotifications;
            ViewBag.ActionNotificationCount = actionNotifications.Count;
            ViewBag.ArchivedActionNotificationCount = archivedActionNotifications.Count;
            ViewBag.PendingActionCount = actionNotifications.Count(x => x.RequiresAction);
            ViewBag.CriticalActionCount = actionNotifications.Count(x => x.Priority == "HIGH");

            try
            {
                const int pageSize = 50;
                page = Math.Max(page, 1);

                var query = _context.VwNotificationCenters
                    .AsNoTracking()
                    .OrderByDescending(x => x.CreatedAt);

                var totalCount = await query.CountAsync();
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
                page = Math.Min(page, totalPages);

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalRecords = totalCount;

                return View(notifications);
            }
            catch
            {

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
                ViewBag.TotalRecords = 0;

                return View(new List<VwNotificationCenter>());
            }
        }































































































public async Task<IActionResult> RefundDetails(long id)
{
    // Refund actions keep an audit trail and update the booking/payment state connected to the refund.
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


[HttpPost]
public async Task<IActionResult> ApproveRefund(long id)
{
    var now =
        DatabaseTimestampNow();


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


    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == id);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }


    refund.refund_status = "SUCCESS";

    refund.workflow_action =
        "APPROVED BY ADMIN";

    refund.processed_at =
        now;

    refund.updated_at =
        now;


    refund.approved_by =
        HttpContext.Session.GetString("UserName");

    refund.approved_at =
        now;

    refund.admin_notes =
        "Refund approved by admin";


    _context.RefundActionLogs.Add(
        new RefundActionLog
        {
            refund_id = refund.id,

            refund_ref = refund.refund_ref,

            action_name = "APPROVE_REFUND",

            action_by =
                HttpContext.Session.GetString("UserName"),

            action_time =
                now,

            action_notes =
                "Refund approved successfully",

            ip_address =
                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

            created_at =
                now
        });


    await _context.SaveChangesAsync();


    await _activityLogger.LogAsync(
        action: "APPROVE_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund approved: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );


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


[HttpPost]
public async Task<IActionResult> RejectRefund(long id)
{
    var now =
        DatabaseTimestampNow();

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


    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == id);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }

    await using var dbTransaction =
        await _context.Database.BeginTransactionAsync();

    refund.refund_status = "REJECTED";

    refund.workflow_action =
        "REJECTED BY ADMIN - TICKET REMAINS ACTIVE";

    refund.updated_at =
        now;


    refund.rejected_by =
        HttpContext.Session.GetString("UserName");

    refund.rejected_at =
        now;

    refund.admin_notes =
        "Refund rejected by admin. Ticket cancellation request denied and ticket remains active.";

    await RestoreBookingAfterRefundRejection(refund);


    _context.RefundActionLogs.Add(
        new RefundActionLog
        {
            refund_id = refund.id,

            refund_ref = refund.refund_ref,

            action_name = "REJECT_REFUND",

            action_by =
                HttpContext.Session.GetString("UserName"),

            action_time =
                now,

            action_notes =
                "Refund rejected by admin. Ticket remains active.",

            ip_address =
                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

            created_at =
                now
        });


    await _context.SaveChangesAsync();
    await dbTransaction.CommitAsync();


    await _activityLogger.LogAsync(
        action: "REJECT_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund rejected and ticket kept active: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );


    var notificationResult =
        await TrySendRefundRejectionUserNotification(refund);

    await _activityLogger.LogAsync(
        action: "REFUND_NOTIFICATION",
        module: "NOTIFICATION",
        entityType: "REFUND",
        description:
            notificationResult.Success
                ? $"Rejection email sent for refund {refund.refund_ref}. Ticket remains active."
                : $"Rejection email not sent for refund {refund.refund_ref}: {notificationResult.Message}",
        status: notificationResult.Success ? "SUCCESS" : "FAILED",
        isError: notificationResult.Success ? 0 : 1
    );

    TempData["Success"] =
        notificationResult.Success
            ? "Refund rejected successfully. Ticket remains active and the user was notified by email."
            : $"Refund rejected successfully. Ticket remains active, but email notification failed: {notificationResult.Message}";

    return RedirectToAction("Refunds");
}

private async Task RestoreBookingAfterRefundRejection(Refund refund)
{
    var now =
        DatabaseTimestampNow();

    var booking =
        await _context.Bookings
        .FirstOrDefaultAsync(x => x.Id == refund.booking_id);

    if (booking != null)
    {
        booking.BookingStatus =
            "CONFIRMED";

        booking.PaymentStatus =
            "SUCCESS";

        booking.RefundStatus =
            "REJECTED";

        booking.CancelledAt =
            null;

        booking.CancellationReason =
            "Cancellation rejected by admin. Ticket remains active.";

        booking.UpdatedAt =
            now;
    }

    var transaction =
        await _context.Transactions
        .FirstOrDefaultAsync(x => x.Id == refund.transaction_id);

    if (transaction != null)
    {
        transaction.RefundStatus =
            "REJECTED";

        transaction.RefundedAmount =
            Math.Max(0, (transaction.RefundedAmount ?? 0) - refund.refund_amount);

        transaction.UpdatedAt =
            now;
    }

    var tickets =
        await _context.Tickets
        .Where(x => x.BookingId == refund.booking_id)
        .ToListAsync();

    foreach (var ticket in tickets)
    {
        ticket.TicketStatus =
            "ACTIVE";

        ticket.UpdatedAt =
            now;
    }

    var bookingSeats =
        await _context.BookingSeats
        .Where(x => x.BookingId == refund.booking_id)
        .ToListAsync();

    foreach (var seat in bookingSeats)
    {
        seat.BookingStatus =
            "BOOKED";
    }

    if (booking != null && bookingSeats.Count > 0)
    {
        var seatIds =
            bookingSeats
            .Select(x => x.ScreenSeatId)
            .ToList();

        var locks =
            await _context.SeatLocks
            .Where(x =>
                x.ScheduleId == booking.ScheduleId &&
                seatIds.Contains(x.ScreenSeatId))
            .ToListAsync();

        foreach (var seatLock in locks)
        {
            seatLock.LockStatus =
                "CONFIRMED";
        }
    }
}

        private static DateTime DatabaseTimestampNow()
        {
            return DateTime.SpecifyKind(
                DateTime.UtcNow,
                DateTimeKind.Unspecified);
        }

        private async Task EnsureAdminReportingViews()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
DROP VIEW IF EXISTS public.vw_enterprise_activity_logs;
DROP VIEW IF EXISTS public.vw_refund_summary;
DROP VIEW IF EXISTS public.vw_notification_center;
DROP VIEW IF EXISTS public.vw_booking_transaction_summary;
DROP VIEW IF EXISTS public.vw_booking_complete_details;

CREATE OR REPLACE VIEW public.vw_booking_complete_details AS
SELECT
    b.id AS booking_id,
    COALESCE(b.booking_ref::varchar(100), ('Booking #' || b.id::text)::varchar(100)) AS booking_ref,
    COALESCE(u.""Id"", b.user_id)::integer AS user_id,
    COALESCE(u.""Name"", 'User #' || b.user_id::text) AS user_name,
    COALESCE(u.""Email"", 'Unavailable') AS user_email,
    COALESCE(ss.""Type"", 'Unknown') AS show_type,
    COALESCE(m.""Title"", st.""Title"", ls.""Title"", 'Untitled Show') AS show_title,
    COALESCE(l.""Area"", 'N/A') AS location_name,
    COALESCE(ss.""StartTime"", b.booked_at, b.created_at) AS start_time,
    COALESCE(seat_list.seat_numbers, 'N/A') AS seat_numbers,
    COALESCE(b.booking_status, 'PENDING') AS booking_status,
    COALESCE(b.payment_status, 'PENDING') AS payment_status,
    COALESCE(b.total_tickets, 0) AS total_tickets,
    COALESCE(b.total_amount, 0) AS total_amount,
    b.tax_amount,
    b.discount_amount,
    b.payable_amount,
    COALESCE(tx.transaction_ref::text, bt.""TransactionRef""::text, 'N/A') AS transaction_ref,
    COALESCE(tx.payment_method::text, bt.""PaymentMethod""::text, 'N/A') AS payment_method,
    COALESCE(tx.gateway_name::text, CASE WHEN bt.""Id"" IS NOT NULL THEN 'DUMMY_GATEWAY' END, 'N/A') AS gateway_name,
    COALESCE(tx.status::text, bt.""PaymentStatus""::text, 'PENDING') AS transaction_status,
    COALESCE(b.booked_at, b.created_at) AS booked_at,
    b.confirmed_at,
    b.cancelled_at,
    COALESCE(b.created_at, b.booked_at, CURRENT_TIMESTAMP)::timestamp without time zone AS created_at,
    CASE WHEN b.booking_status = 'FAILED' THEN 1 ELSE 0 END AS is_error
FROM public.bookings b
LEFT JOIN public.""Users"" u ON b.user_id = u.""Id""
LEFT JOIN public.""ShowSchedules"" ss ON b.schedule_id = ss.""Id""
LEFT JOIN public.""Movies"" m ON ss.""MovieId"" = m.""Id""
LEFT JOIN public.""StandupShows"" st ON ss.""StandupShowId"" = st.""Id""
LEFT JOIN public.""LiveStreams"" ls ON ss.""LiveStreamId"" = ls.""Id""
LEFT JOIN public.""Locations"" l ON ss.""LocationId"" = l.""Id""
LEFT JOIN LATERAL (
    SELECT string_agg(tk.seat_number, ', ' ORDER BY tk.seat_number) AS seat_numbers
    FROM public.tickets tk
    WHERE tk.booking_id = b.id
) seat_list ON true
LEFT JOIN LATERAL (
    SELECT t.*
    FROM public.transactions t
    WHERE t.id = b.transaction_id OR t.booking_id = b.id
    ORDER BY
        CASE WHEN t.id = b.transaction_id THEN 0 ELSE 1 END,
        t.completed_at DESC NULLS LAST,
        t.created_at DESC NULLS LAST
    LIMIT 1
) tx ON true
LEFT JOIN LATERAL (
    SELECT bt_inner.*
    FROM public.booking_transactions bt_inner
    WHERE bt_inner.""BookingId"" = b.id
    ORDER BY bt_inner.""PaidAt"" DESC NULLS LAST, bt_inner.""CreatedAt"" DESC NULLS LAST
    LIMIT 1
) bt ON true;

CREATE OR REPLACE VIEW public.vw_booking_transaction_summary AS
SELECT
    b.id AS booking_id,
    COALESCE(b.booking_ref::varchar(100), ('Booking #' || b.id::text)::varchar(100)) AS booking_ref,
    COALESCE(u.""Id"", b.user_id)::integer AS user_id,
    COALESCE(u.""Name"", 'User #' || b.user_id::text) AS user_name,
    COALESCE(u.""Email"", 'Unavailable') AS user_email,
    COALESCE(s.""Type"", 'Unknown') AS show_type,
    COALESCE(m.""Title"", ss.""Title"", ls.""Title"", 'Untitled Show') AS show_title,
    COALESCE(b.booking_status, '') AS booking_status,
    COALESCE(tx.id, bt.""Id"") AS transaction_id,
    COALESCE(tx.transaction_ref::text, bt.""TransactionRef""::text, '') AS transaction_ref,
    COALESCE(tx.payment_method::text, bt.""PaymentMethod""::text, '') AS payment_method,
    COALESCE(tx.amount, bt.""Amount"", 0) AS transaction_amount,
    COALESCE(tx.currency, 'INR') AS currency,
    COALESCE(tx.status::text, bt.""PaymentStatus""::text, '') AS transaction_status,
    COALESCE(tx.gateway_name::text, CASE WHEN bt.""Id"" IS NOT NULL THEN 'DUMMY_GATEWAY' END, '') AS gateway_name,
    COALESCE(tx.failure_reason, '') AS failure_reason,
    CASE WHEN lower(COALESCE(tx.status::text, bt.""PaymentStatus""::text, '')) = 'failed' THEN 1 ELSE 0 END AS is_payment_error,
    COALESCE(b.total_amount, 0) AS total_amount,
    COALESCE(b.created_at, b.booked_at, CURRENT_TIMESTAMP)::timestamp without time zone AS booking_created_at,
    COALESCE(tx.completed_at, bt.""PaidAt""::timestamp without time zone) AS completed_at
FROM public.bookings b
LEFT JOIN LATERAL (
    SELECT t.*
    FROM public.transactions t
    WHERE t.id = b.transaction_id OR t.booking_id = b.id
    ORDER BY
        CASE WHEN t.id = b.transaction_id THEN 0 ELSE 1 END,
        t.completed_at DESC NULLS LAST,
        t.created_at DESC NULLS LAST
    LIMIT 1
) tx ON true
LEFT JOIN LATERAL (
    SELECT bt_inner.*
    FROM public.booking_transactions bt_inner
    WHERE bt_inner.""BookingId"" = b.id
    ORDER BY bt_inner.""PaidAt"" DESC NULLS LAST, bt_inner.""CreatedAt"" DESC NULLS LAST
    LIMIT 1
) bt ON true
LEFT JOIN public.""Users"" u ON b.user_id = u.""Id""
LEFT JOIN public.""ShowSchedules"" s ON b.schedule_id = s.""Id""
LEFT JOIN public.""Movies"" m ON s.""MovieId"" = m.""Id""
LEFT JOIN public.""StandupShows"" ss ON s.""StandupShowId"" = ss.""Id""
LEFT JOIN public.""LiveStreams"" ls ON s.""LiveStreamId"" = ls.""Id"";

CREATE OR REPLACE VIEW public.vw_notification_center AS
SELECT
    un.id AS notification_id,
    COALESCE(u.""Name"", 'User #' || un.user_id::text) AS user_name,
    COALESCE(u.""Email"", un.recipient_email, 'Unavailable') AS user_email,
    COALESCE(nt.template_code, 'CUSTOM'::varchar) AS template_code,
    COALESCE(nt.template_name, left(COALESCE(un.title, 'Notification'), 180)::varchar) AS template_name,
    un.notification_type,
    un.title,
    un.message,
    un.status,
    un.priority,
    un.sent_at,
    un.delivered_at,
    un.read_at,
    un.retry_count,
    un.failure_reason,
    un.created_at,
    CASE WHEN un.status = 'FAILED' THEN 1 ELSE 0 END AS is_error
FROM public.user_notifications un
LEFT JOIN public.""Users"" u ON un.user_id = u.""Id""
LEFT JOIN public.notification_templates nt ON un.template_id = nt.id;

CREATE OR REPLACE VIEW public.vw_refund_summary AS
SELECT
    r.id AS refund_id,
    r.refund_ref,
    COALESCE(b.booking_ref::varchar(100), ('Booking #' || r.booking_id::text)::varchar(100)) AS booking_ref,
    COALESCE(t.transaction_ref::varchar(100), ('Transaction #' || r.transaction_id::text)::varchar(100)) AS transaction_ref,
    COALESCE(u.""Id"", r.user_id)::integer AS user_id,
    COALESCE(u.""Name"", 'User #' || r.user_id::text) AS user_name,
    COALESCE(u.""Email"", 'Unavailable') AS user_email,
    r.refund_amount,
    r.refund_reason,
    r.refund_status,
    r.refund_method,
    r.workflow_action,
    r.approved_by,
    r.approved_at,
    r.rejected_by,
    r.rejected_at,
    r.retried_by,
    r.retried_at,
    r.gateway_refund_id,
    r.failure_reason,
    r.requested_at,
    r.processed_at,
    r.created_at,
    r.updated_at,
    r.admin_notes,
    CASE WHEN r.refund_status = 'SUCCESS' THEN 0 ELSE 1 END AS is_refund_error
FROM public.refunds r
LEFT JOIN public.bookings b ON r.booking_id = b.id
LEFT JOIN public.transactions t ON r.transaction_id = t.id
LEFT JOIN public.""Users"" u ON r.user_id = u.""Id"";

CREATE OR REPLACE VIEW public.vw_enterprise_activity_logs AS
SELECT u.""Id""::bigint AS entity_id, 'USER'::text AS module, 'USER_REGISTERED'::varchar AS action,
       COALESCE(u.""Name"", 'User #' || u.""Id""::text) AS user_name, COALESCE(u.""Email"", 'Unavailable') AS user_email, NULL::text AS reference_no,
       'SUCCESS'::varchar AS status, 'New user account created'::text AS description,
       NULL::text AS error_message, u.""CreatedAt""::timestamp with time zone AS activity_time
FROM public.""Users"" u
UNION ALL
SELECT t.id, 'TRANSACTION', t.status, COALESCE(u.""Name"", 'User #' || t.user_id::text), COALESCE(u.""Email"", 'Unavailable'), t.transaction_ref, t.status,
       COALESCE(t.description, 'Transaction processed'), t.failure_reason, COALESCE(t.completed_at, t.created_at)::timestamp with time zone
FROM public.transactions t
LEFT JOIN public.""Users"" u ON u.""Id"" = t.user_id
UNION ALL
SELECT b.id, 'BOOKING', b.booking_status, COALESCE(u.""Name"", 'User #' || b.user_id::text), COALESCE(u.""Email"", 'Unavailable'), b.booking_ref, b.booking_status,
       'Ticket booking activity', NULL::text, COALESCE(b.created_at, b.booked_at)::timestamp with time zone
FROM public.bookings b
LEFT JOIN public.""Users"" u ON u.""Id"" = b.user_id
UNION ALL
SELECT al.id::bigint, 'SYSTEM', al.action::varchar, COALESCE(u.""Name"", 'System'), COALESCE(u.""Email"", 'Unavailable'), NULL::text, al.status::varchar,
       al.description, al.error_message, al.created_at::timestamp with time zone
FROM public.activity_logs al
LEFT JOIN public.""Users"" u ON u.""Id"" = al.user_id;");
        }

        private async Task<OtpDeliveryResult> SendRefundRejectionUserNotification(Refund refund)
        {
    if (refund.user_id > int.MaxValue || refund.user_id < int.MinValue)
    {
        return new OtpDeliveryResult(false, false, "Refund user id is invalid.");
    }

    var userId =
        (int)refund.user_id;

    var user =
        await _context.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == userId);

    if (user == null || string.IsNullOrWhiteSpace(user.Email))
    {
        return new OtpDeliveryResult(false, false, "User email not found.");
    }

    var booking =
        await _context.Bookings
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == refund.booking_id);

    var bookingRef =
        string.IsNullOrWhiteSpace(booking?.BookingRef)
            ? $"Booking #{refund.booking_id}"
            : booking.BookingRef;

    var subject =
        $"Ticket cancellation request rejected - {bookingRef}";

    var message =
        $"Hello {NullText(user.Name)},\n\n" +
        $"Your cancellation request for {bookingRef} was reviewed by admin and rejected.\n" +
        "Your ticket cannot be cancelled, so the ticket remains active and can still be used for the show.\n\n" +
        $"Refund reference: {refund.refund_ref}\n" +
        $"Refund amount requested: {CurrencyFormatter.FormatRupees(refund.refund_amount)}\n\n" +
        "Thank you,\nshowTime";

    var emailResult =
        await _emailDeliveryService.SendEmailAsync(
            user.Email,
            subject,
            message);

    var notificationStatus =
        emailResult.Success
            ? "SENT"
            : "FAILED";

    var sentAt =
        emailResult.Success
            ? DateTime.UtcNow
            : (DateTime?)null;

    var failureReason =
        emailResult.Success
            ? null
            : emailResult.Message;

    await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO user_notifications
(
    user_id,
    template_id,
    notification_type,
    title,
    message,
    status,
    priority,
    recipient_email,
    sent_at,
    delivered_at,
    retry_count,
    failure_reason,
    created_at,
    updated_at
)
VALUES
(
    {refund.user_id},
    NULL,
    'EMAIL',
    {subject},
    {message},
    {notificationStatus},
    'HIGH',
    {user.Email},
    {sentAt},
    {sentAt},
    0,
    {failureReason},
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);");

    return emailResult;
}

private async Task<OtpDeliveryResult> TrySendRefundRejectionUserNotification(Refund refund)
{
    try
    {
        return await SendRefundRejectionUserNotification(refund);
    }
    catch (Exception ex)
    {
        return new OtpDeliveryResult(false, true, ex.Message);
    }
}


[HttpPost]
public async Task<IActionResult> RetryRefund(long id)
{
    var now =
        DatabaseTimestampNow();

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


    var refund = await _context.Refunds
        .FirstOrDefaultAsync(x => x.id == id);

    if (refund == null)
    {
        TempData["Error"] =
            "Refund not found.";

        return RedirectToAction("Refunds");
    }


    refund.refund_status = "PENDING";

    refund.workflow_action =
        "RETRIED BY ADMIN";

    refund.failure_reason = null;

    refund.updated_at =
        now;


    refund.retried_by =
        HttpContext.Session.GetString("UserName");

    refund.retried_at =
        now;

    refund.admin_notes =
        "Refund retry initiated by admin";


    _context.RefundActionLogs.Add(
        new RefundActionLog
        {
            refund_id = refund.id,

            refund_ref = refund.refund_ref,

            action_name = "RETRY_REFUND",

            action_by =
                HttpContext.Session.GetString("UserName"),

            action_time =
                now,

            action_notes =
                "Refund retry initiated",

            ip_address =
                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

            created_at =
                now
        });


    await _context.SaveChangesAsync();


    await _activityLogger.LogAsync(
        action: "RETRY_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund retry initiated: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );


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
        DatabaseTimestampNow();

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


public IActionResult ExportRefunds()
{
    var refunds = _context.VwRefundSummaries
        .AsNoTracking()
        .ToList();

    var builder =
        new System.Text.StringBuilder();


    builder.AppendLine(
        "RefundRef,BookingRef,TransactionRef,UserName,UserEmail,RefundAmount,RefundStatus,RefundMethod,RequestedAt");


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


    return File(
        System.Text.Encoding.UTF8.GetBytes(
            builder.ToString()),
        "text/csv",
        $"refunds_{DateTime.Now:yyyyMMddHHmmss}.csv"
    );
}

public async Task<IActionResult> CouponUsage(int page = 1)
{
    await EnsureAdminShowInfrastructure();
    await EnsureAdminReportingViews();

    const int pageSize = 50;
    page = Math.Max(page, 1);

    var query = _context.VwCouponUsages
        .AsNoTracking()
        .OrderByDescending(x => x.UsedAt);

    var totalCount = await query.CountAsync();
    var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
    page = Math.Min(page, totalPages);

    var rows = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.TotalRecords = totalCount;

    return View(rows);
}

public async Task<IActionResult> Versions(int page = 1)
{
    await EnsureAdminShowInfrastructure();
    await SyncApplicationVersionsFromGit();

    if (!await _context.ApplicationVersions.AnyAsync())
    {
        _context.ApplicationVersions.Add(new ApplicationVersion
        {
            VersionNumber = "1.0.0",
            ReleaseTitle = "Initial application version",
            ReleaseNotes = "Admin managed release record.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = HttpContext.Session.GetString("UserName") ?? "System",
            IsCurrent = true
        });
        await _context.SaveChangesAsync();
    }

    const int pageSize = 50;
    page = Math.Max(page, 1);

    var query = _context.ApplicationVersions
        .AsNoTracking()
        .OrderByDescending(x => x.IsCurrent)
        .ThenByDescending(x => x.UpdatedAt);

    var totalCount = await query.CountAsync();
    var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
    page = Math.Min(page, totalPages);

    var versions = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = totalPages;
    ViewBag.TotalRecords = totalCount;

    return View(versions);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateVersion(long id, string versionNumber, string releaseTitle, string? releaseNotes, bool isCurrent)
{
    await EnsureAdminShowInfrastructure();

    var version = await _context.ApplicationVersions.FirstOrDefaultAsync(x => x.Id == id);
    if (version == null)
    {
        TempData["Error"] = "Version not found.";
        return RedirectToAction(nameof(Versions));
    }

    if (string.IsNullOrWhiteSpace(versionNumber) || string.IsNullOrWhiteSpace(releaseTitle))
    {
        TempData["Error"] = "Version number and title are required.";
        return RedirectToAction(nameof(Versions));
    }

    if (isCurrent)
    {
        var currentVersions = await _context.ApplicationVersions
            .Where(x => x.IsCurrent && x.Id != id)
            .ToListAsync();

        foreach (var item in currentVersions)
        {
            item.IsCurrent = false;
        }
    }

    version.VersionNumber = versionNumber.Trim();
    version.ReleaseTitle = releaseTitle.Trim();
    version.ReleaseNotes = releaseNotes?.Trim();
    version.IsCurrent = isCurrent;
    version.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    TempData["Success"] = "Version updated.";
    return RedirectToAction(nameof(Versions));
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteVersion(long id)
{
    await EnsureAdminShowInfrastructure();

    var version = await _context.ApplicationVersions.FirstOrDefaultAsync(x => x.Id == id);
    if (version == null)
    {
        TempData["Error"] = "Version not found.";
        return RedirectToAction(nameof(Versions));
    }

    if (await _context.ApplicationVersions.CountAsync() <= 1)
    {
        TempData["Error"] = "Keep at least one application version.";
        return RedirectToAction(nameof(Versions));
    }

    var wasCurrent = version.IsCurrent;
    _context.ApplicationVersions.Remove(version);
    await _context.SaveChangesAsync();

    if (wasCurrent)
    {
        var latest = await _context.ApplicationVersions
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync();

        if (latest != null)
        {
            latest.IsCurrent = true;
            latest.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    TempData["Success"] = "Version deleted.";
    return RedirectToAction(nameof(Versions));
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateVersion(string versionNumber, string releaseTitle, string? releaseNotes, bool isCurrent)
{
    await EnsureAdminShowInfrastructure();

    if (string.IsNullOrWhiteSpace(versionNumber) || string.IsNullOrWhiteSpace(releaseTitle))
    {
        TempData["Error"] = "Version number and title are required.";
        return RedirectToAction(nameof(Versions));
    }

    if (isCurrent)
    {
        var current = await _context.ApplicationVersions.Where(x => x.IsCurrent).ToListAsync();
        foreach (var item in current)
        {
            item.IsCurrent = false;
        }
    }

    _context.ApplicationVersions.Add(new ApplicationVersion
    {
        VersionNumber = versionNumber.Trim(),
        ReleaseTitle = releaseTitle.Trim(),
        ReleaseNotes = releaseNotes?.Trim(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = HttpContext.Session.GetString("UserName") ?? "Admin",
        IsCurrent = isCurrent
    });

    await _context.SaveChangesAsync();
    TempData["Success"] = "Application version saved.";
    return RedirectToAction(nameof(Versions));
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateNextVersion()
{
    await EnsureAdminShowInfrastructure();

    var latest =
    await _context.ApplicationVersions
    .OrderByDescending(x=>x.IsCurrent)
    .ThenByDescending(x=>x.UpdatedAt)
    .FirstOrDefaultAsync();

    var nextVersion =
    IncrementPatchVersion(latest?.VersionNumber);

    var current =
    await _context.ApplicationVersions
    .Where(x=>x.IsCurrent)
    .ToListAsync();

    foreach(var item in current)
    {
        item.IsCurrent=false;
    }

    var now =
    DateTime.UtcNow;

    _context.ApplicationVersions.Add(new ApplicationVersion
    {
        VersionNumber=nextVersion,
        ReleaseTitle=$"Release {nextVersion}",
        ReleaseNotes=$"Auto-created on {now:yyyy-MM-dd HH:mm:ss} UTC.",
        CreatedAt=now,
        UpdatedAt=now,
        CreatedBy=HttpContext.Session.GetString("UserName") ?? "Admin",
        IsCurrent=true
    });

    await _context.SaveChangesAsync();
    TempData["Success"]=$"Version {nextVersion} saved.";
    return RedirectToAction(nameof(Versions));
}

private static string IncrementPatchVersion(string? versionNumber)
{
    var parts =
    (versionNumber ?? "1.0.0")
    .Split('.',StringSplitOptions.RemoveEmptyEntries)
    .Select(part=>int.TryParse(part,out var number) ? number : 0)
    .ToList();

    while(parts.Count<3)
    {
        parts.Add(0);
    }

    parts[2]++;

    if(parts[2]>9)
    {
        parts[2]=0;
        parts[1]++;
    }

    if(parts[1]>9)
    {
        parts[1]=0;
        parts[0]++;
    }

    return $"{parts[0]}.{parts[1]}.{parts[2]}";
}

private async Task SyncApplicationVersionsFromGit()
{
    var commits =
    await GetGitVersionCommits();

    if(commits.Count==0)
    {
        return;
    }

    var releaseNotes =
    await _context.ApplicationVersions
    .AsNoTracking()
    .Where(x=>x.ReleaseNotes!=null && x.ReleaseNotes.Contains("Git commit:"))
    .Select(x=>x.ReleaseNotes!)
    .ToListAsync();

    var importedHashes =
    releaseNotes
    .SelectMany(ExtractGitHashes)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var newCommits =
    commits
    .Where(commit=>!importedHashes.Contains(commit.ShortHash) && !importedHashes.Contains(commit.Hash))
    .ToList();

    if(newCommits.Count==0)
    {
        return;
    }

    var latest =
    await _context.ApplicationVersions
    .OrderByDescending(x=>x.IsCurrent)
    .ThenByDescending(x=>x.UpdatedAt)
    .FirstOrDefaultAsync();

    var latestVersion =
    latest?.VersionNumber;

    var currentVersions =
    await _context.ApplicationVersions
    .Where(x=>x.IsCurrent)
    .ToListAsync();

    foreach(var item in currentVersions)
    {
        item.IsCurrent=false;
    }

    foreach(var commit in newCommits)
    {
        latestVersion =
        IncrementPatchVersion(latestVersion);

        _context.ApplicationVersions.Add(new ApplicationVersion
        {
            VersionNumber=latestVersion,
            ReleaseTitle=TrimVersionTitle(commit.Subject),
            ReleaseNotes=$"Git commit: {commit.ShortHash} | Committed: {commit.CommittedAt.ToLocalTime():yyyy-MM-ddTHH:mm:sszzz}",
            CreatedAt=commit.CommittedAt.UtcDateTime,
            UpdatedAt=commit.CommittedAt.UtcDateTime,
            CreatedBy="Git",
            IsCurrent=commit==newCommits[^1]
        });
    }

    await _context.SaveChangesAsync();
}

private static async Task<List<GitVersionCommit>> GetGitVersionCommits()
{
    try
    {
        var revision =
        "HEAD";

        var upstream =
        await RunGitCommand("rev-parse","--abbrev-ref","--symbolic-full-name","@{u}");

        if(upstream.ExitCode==0 && !string.IsNullOrWhiteSpace(upstream.Output))
        {
            revision=upstream.Output.Trim();
        }

        var log =
        await RunGitCommand("log","--reverse","--format=%H%x1f%ct%x1f%s",revision);

        if(log.ExitCode!=0 || string.IsNullOrWhiteSpace(log.Output))
        {
            return new List<GitVersionCommit>();
        }

        return log.Output
        .Split('\n',StringSplitOptions.RemoveEmptyEntries)
        .Select(ParseGitVersionCommit)
        .Where(commit=>commit!=null)
        .Select(commit=>commit!)
        .ToList();
    }
    catch
    {
        return new List<GitVersionCommit>();
    }
}

private static async Task<GitCommandResult> RunGitCommand(params string[] arguments)
{
    using var process =
    new Process();

    process.StartInfo.FileName="git";
    process.StartInfo.WorkingDirectory=Directory.GetCurrentDirectory();
    process.StartInfo.RedirectStandardOutput=true;
    process.StartInfo.RedirectStandardError=true;
    process.StartInfo.UseShellExecute=false;

    foreach(var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    process.Start();

    var outputTask =
    process.StandardOutput.ReadToEndAsync();

    var errorTask =
    process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    return new GitCommandResult(
    process.ExitCode,
    await outputTask,
    await errorTask);
}

private static GitVersionCommit? ParseGitVersionCommit(string line)
{
    var parts =
    line.Split('\u001f');

    if(parts.Length<3 || string.IsNullOrWhiteSpace(parts[0]))
    {
        return null;
    }

    var committedAt =
    long.TryParse(parts[1],out var unixSeconds)
    ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
    : DateTimeOffset.UtcNow;

    return new GitVersionCommit(
    parts[0],
    parts[0].Length>12 ? parts[0][..12] : parts[0],
    committedAt,
    string.Join(" ",parts.Skip(2)).Trim());
}

private static IEnumerable<string> ExtractGitHashes(string releaseNotes)
{
    const string marker =
    "Git commit:";

    var markerIndex =
    releaseNotes.IndexOf(marker,StringComparison.OrdinalIgnoreCase);

    if(markerIndex<0)
    {
        yield break;
    }

    var hashStart =
    markerIndex+marker.Length;

    var hash =
    new string(
    releaseNotes[hashStart..]
    .TrimStart()
    .TakeWhile(Uri.IsHexDigit)
    .ToArray());

    if(!string.IsNullOrWhiteSpace(hash))
    {
        yield return hash;
    }
}

private static string TrimVersionTitle(string subject)
{
    const int maxLength =
    255;

    var title =
    string.IsNullOrWhiteSpace(subject)
    ? "Git release"
    : subject.Trim();

    return title.Length<=maxLength
    ? title
    : title[..maxLength];
}

private sealed record GitVersionCommit(
string Hash,
string ShortHash,
DateTimeOffset CommittedAt,
string Subject);

private sealed record GitCommandResult(
int ExitCode,
string Output,
string Error);

public async Task<IActionResult> ContentManager()
{
    await EnsureNewsAdminSchema();

    ViewBag.Movies = await _context.Movies
        .AsNoTracking()
        .OrderByDescending(x => x.Id)
        .Take(100)
        .ToListAsync();

    ViewBag.StandupShows = await _context.StandupShows
        .AsNoTracking()
        .OrderByDescending(x => x.Id)
        .Take(100)
        .ToListAsync();

    ViewBag.LiveStreams = await _context.LiveStreams
        .AsNoTracking()
        .OrderByDescending(x => x.Id)
        .Take(100)
        .ToListAsync();

    ViewBag.NewsChannels = await _context.NewsChannels
        .AsNoTracking()
        .OrderBy(x => x.SortOrder)
        .ThenBy(x => x.ChannelName)
        .Take(120)
        .ToListAsync();

    return View();
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SaveContent(
    string contentType,
    int? id,
    string title,
    string? directorOrHost,
    string? producer,
    string? cast,
    int duration,
    string? description,
    string? posterUrl,
    string? images,
    string? trailerUrl,
    decimal? imdbRating)
{
    contentType = NormalizeContentType(contentType);
    title = (title ?? string.Empty).Trim();
    duration = Math.Max(duration, 1);

    if (string.IsNullOrWhiteSpace(title))
    {
        TempData["Error"] = "Title is required.";
        return RedirectToAction(nameof(ContentManager));
    }

    if (contentType == "Movie")
    {
        var movie = id.HasValue
            ? await _context.Movies.FirstOrDefaultAsync(x => x.Id == id.Value)
            : new Movie();

        if (movie == null)
        {
            TempData["Error"] = "Movie not found.";
            return RedirectToAction(nameof(ContentManager));
        }

        movie.Title = title;
        movie.Director = directorOrHost?.Trim();
        movie.Producer = string.IsNullOrWhiteSpace(producer) ? "NA" : producer.Trim();
        movie.Cast = string.IsNullOrWhiteSpace(cast) ? "NA" : cast.Trim();
        movie.Duration = duration;
        movie.Description = description?.Trim();
        movie.PosterUrl = posterUrl?.Trim();
        movie.Images = images?.Trim();
        movie.TrailerUrl = trailerUrl?.Trim();
        movie.ImdbRating = imdbRating;

        if (!id.HasValue)
        {
            _context.Movies.Add(movie);
        }
    }
    else if (contentType == "Standup")
    {
        var show = id.HasValue
            ? await _context.StandupShows.FirstOrDefaultAsync(x => x.Id == id.Value)
            : new StandupShow();

        if (show == null)
        {
            TempData["Error"] = "Standup show not found.";
            return RedirectToAction(nameof(ContentManager));
        }

        show.Title = title;
        show.Comedian = string.IsNullOrWhiteSpace(directorOrHost) ? "NA" : directorOrHost.Trim();
        show.Duration = duration;
        show.Description = description?.Trim();
        show.PosterUrl = posterUrl?.Trim();
        show.Images = images?.Trim();
        show.TrailerUrl = trailerUrl?.Trim();

        if (!id.HasValue)
        {
            _context.StandupShows.Add(show);
        }
    }
    else if (contentType == "Live")
    {
        var stream = id.HasValue
            ? await _context.LiveStreams.FirstOrDefaultAsync(x => x.Id == id.Value)
            : new LiveStream();

        if (stream == null)
        {
            TempData["Error"] = "Live stream not found.";
            return RedirectToAction(nameof(ContentManager));
        }

        stream.Title = title;
        stream.Host = string.IsNullOrWhiteSpace(directorOrHost) ? "NA" : directorOrHost.Trim();
        stream.Duration = duration;
        stream.Description = description?.Trim();
        stream.PosterUrl = posterUrl?.Trim();
        stream.Images = images?.Trim();
        stream.TrailerUrl = trailerUrl?.Trim();

        if (!id.HasValue)
        {
            _context.LiveStreams.Add(stream);
        }
    }

    await _context.SaveChangesAsync();
    TempData["Success"] = id.HasValue ? "Content updated." : "Content inserted.";
    return RedirectToAction(nameof(ContentManager));
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteContent(string contentType, int id)
{
    contentType = NormalizeContentType(contentType);

    var isScheduled = contentType switch
    {
        "Movie" => await _context.ShowSchedules.AnyAsync(x => x.MovieId == id),
        "Standup" => await _context.ShowSchedules.AnyAsync(x => x.StandupShowId == id),
        "Live" => await _context.ShowSchedules.AnyAsync(x => x.LiveStreamId == id),
        _ => true
    };

    if (isScheduled)
    {
        TempData["Error"] = "This item is used in schedules. Delete or update schedules first.";
        return RedirectToAction(nameof(ContentManager));
    }

    if (contentType == "Movie")
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(x => x.Id == id);
        if (movie != null) _context.Movies.Remove(movie);
    }
    else if (contentType == "Standup")
    {
        var show = await _context.StandupShows.FirstOrDefaultAsync(x => x.Id == id);
        if (show != null) _context.StandupShows.Remove(show);
    }
    else if (contentType == "Live")
    {
        var stream = await _context.LiveStreams.FirstOrDefaultAsync(x => x.Id == id);
        if (stream != null) _context.LiveStreams.Remove(stream);
    }

    await _context.SaveChangesAsync();
    TempData["Success"] = "Content deleted.";
    return RedirectToAction(nameof(ContentManager));
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SaveNewsChannel(
    long? id,
    string? channelCode,
    string channelName,
    string language,
    string category,
    string region,
    string? country,
    string? state,
    string? city,
    string? description,
    string? logoUrl,
    string? websiteUrl,
    string? liveUrl,
    int sortOrder,
    bool isActive)
{
    await EnsureNewsAdminSchema();

    channelName = (channelName ?? string.Empty).Trim();
    language = string.IsNullOrWhiteSpace(language) ? "All" : language.Trim();
    category = string.IsNullOrWhiteSpace(category) ? "News" : category.Trim();
    region = string.IsNullOrWhiteSpace(region) ? "All" : region.Trim();

    if (string.IsNullOrWhiteSpace(channelName))
    {
        TempData["Error"] = "News channel name is required.";
        return RedirectToAction(nameof(ContentManager), null, null, "news");
    }

    var code = NormalizeCode(string.IsNullOrWhiteSpace(channelCode) ? channelName : channelCode);
    var channel = id.HasValue
        ? await _context.NewsChannels.FirstOrDefaultAsync(x => x.Id == id.Value)
        : new NewsChannel();

    if (channel == null)
    {
        TempData["Error"] = "News channel not found.";
        return RedirectToAction(nameof(ContentManager), null, null, "news");
    }

    var codeExists = await _context.NewsChannels
        .AnyAsync(x => x.ChannelCode == code && (!id.HasValue || x.Id != id.Value));

    if (codeExists)
    {
        TempData["Error"] = "News channel code already exists.";
        return RedirectToAction(nameof(ContentManager), null, null, "news");
    }

    channel.ChannelCode = code;
    channel.ChannelName = channelName;
    channel.Language = language;
    channel.Category = category;
    channel.Region = region;
    channel.Country = string.IsNullOrWhiteSpace(country) ? "India" : country.Trim();
    channel.State = string.IsNullOrWhiteSpace(state) ? "All" : state.Trim();
    channel.City = string.IsNullOrWhiteSpace(city) ? "All" : city.Trim();
    channel.Description = description?.Trim();
    channel.LogoUrl = logoUrl?.Trim();
    channel.WebsiteUrl = websiteUrl?.Trim();
    channel.LiveUrl = liveUrl?.Trim();
    channel.SortOrder = sortOrder;
    channel.IsActive = isActive;
    channel.UpdatedAt = DateTime.UtcNow;

    if (!id.HasValue)
    {
        channel.CreatedAt = DateTime.UtcNow;
        _context.NewsChannels.Add(channel);
    }

    await _context.SaveChangesAsync();
    TempData["Success"] = id.HasValue ? "News channel updated." : "News channel inserted.";
    return RedirectToAction(nameof(ContentManager), null, null, "news");
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteNewsChannel(long id)
{
    await EnsureNewsAdminSchema();

    var channel = await _context.NewsChannels.FirstOrDefaultAsync(x => x.Id == id);
    if (channel == null)
    {
        TempData["Error"] = "News channel not found.";
        return RedirectToAction(nameof(ContentManager), null, null, "news");
    }

    _context.NewsChannels.Remove(channel);
    await _context.SaveChangesAsync();

    TempData["Success"] = "News channel deleted.";
    return RedirectToAction(nameof(ContentManager), null, null, "news");
}

private async Task EnsureNewsAdminSchema()
{
    await _context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS public.news_channels
(
    id bigserial PRIMARY KEY,
    channel_code varchar(80) NOT NULL UNIQUE,
    channel_name varchar(180) NOT NULL,
    language varchar(80) NOT NULL,
    category varchar(80) NOT NULL,
    region varchar(120) NOT NULL,
    country varchar(120) NOT NULL DEFAULT 'India',
    state varchar(120) NOT NULL DEFAULT 'All',
    city varchar(120) NOT NULL DEFAULT 'All',
    description text,
    logo_url text,
    website_url text,
    live_url text,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS country varchar(120) NOT NULL DEFAULT 'India';
ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS state varchar(120) NOT NULL DEFAULT 'All';
ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS city varchar(120) NOT NULL DEFAULT 'All';
");
}

private static string NormalizeContentType(string? contentType)
{
    var value = (contentType ?? string.Empty).Trim().ToLowerInvariant();

    if (value.Contains("standup"))
    {
        return "Standup";
    }

    if (value.Contains("live") || value.Contains("music"))
    {
        return "Live";
    }

    return "Movie";
}



public IActionResult UserDetails(long id)
{
    var user = _context.Users
        .FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return NotFound();
    }


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


    var transactions = _context.VwBookingTransactionSummaries
        .Where(x => x.UserId == id)
        .OrderByDescending(x => x.BookingCreatedAt)
        .ToList();

    var bookings = _context.VwBookingCompleteDetails
        .Where(x => x.UserId == id)
        .OrderByDescending(x => x.BookedAt)
        .ToList();


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


    var wallet = _context.VwWalletSummaries
        .FirstOrDefault(x => x.UserId == id);


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


        ProfileImagePath =
            string.IsNullOrWhiteSpace(user.ProfileImagePath)
                ? "/images/default-user.png"
                : user.ProfileImagePath,

        IsActive = user.is_active,

        IsDeleted = user.is_deleted,

        RegisteredAt = user.CreatedAt,

        LastLoginAt =
    user.UpdatedAt ?? user.CreatedAt,


        WalletBalance =
            wallet?.WalletBalance ?? 0,

        BlockedBalance =
            wallet?.BlockedBalance ?? 0,

        WalletCredits =
            wallet?.TotalCredits ?? 0,

        WalletDebits =
            wallet?.TotalDebits ?? 0,

        LoyaltyPoints =
            wallet?.LoyaltyPoints ?? 0,

        WalletStatus =
            wallet?.WalletStatus ?? "NA",

        TotalWalletTransactions =
            wallet?.TotalWalletTransactions ?? 0,

        LastWalletTransactionAt =
            wallet?.LastTransactionAt,


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


        LastTransactions = transactions,

        Bookings = bookings,


        RecentActivities = recentActivities,


        UserAccess = userRoles
    };

    return View(model);
}


public IActionResult UserAccess()
{

    var users = _context.Users
        .AsNoTracking()
        .Where(x => !x.is_deleted)
        .OrderBy(x => x.Name)
        .ToList();


    var mappings = _context.UserRoleMappings
        .AsNoTracking()
        .Where(x => x.IsActive)
        .ToList();


    var roles = _context.Roles
        .AsNoTracking()
        .ToList();


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


        RoleIds = mappings
            .Where(x => x.UserId == user.Id)
            .Select(x => x.RoleId)
            .Distinct()
            .ToList(),


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
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddUserRole(UserRoleUpdateViewModel request)
{

    var userExists =
        _context.Users.Any(x =>
            x.Id == request.UserId);

    if (!userExists)
    {
        TempData["Error"] =
            "User not found.";

        return RedirectToAction("UserAccess");
    }


    var requestedRole =
        _context.Roles.FirstOrDefault(x =>
            x.Id == request.RoleId &&
            x.IsActive);

    if (requestedRole == null)
    {
        TempData["Error"] =
            "Role not found.";

        return RedirectToAction("UserAccess");
    }

    var activeUserRoles =
        _context.UserRoleMappings
            .Where(x =>
                x.UserId == request.UserId &&
                x.IsActive)
            .Join(
                _context.Roles,
                map => map.RoleId,
                role => role.Id,
                (map, role) => role.RoleCode)
            .ToList();

    var hasDumAdmin =
        activeUserRoles.Any(roleCode =>
            string.Equals(roleCode, "DUM_ADMIN", StringComparison.OrdinalIgnoreCase));
    var hasOtherRoles =
        activeUserRoles.Any(roleCode =>
            !string.Equals(roleCode, "DUM_ADMIN", StringComparison.OrdinalIgnoreCase));
    var isGrantingDumAdmin =
        string.Equals(requestedRole.RoleCode, "DUM_ADMIN", StringComparison.OrdinalIgnoreCase);

    if ((isGrantingDumAdmin && hasOtherRoles) || (!isGrantingDumAdmin && hasDumAdmin))
    {
        TempData["Error"] =
            "dum_Admin role cannot be combined with any other role. Remove existing roles first.";

        return RedirectToAction("UserAccess");
    }


    var existingMapping =
        _context.UserRoleMappings
            .FirstOrDefault(x =>
                x.UserId == request.UserId &&
                x.RoleId == request.RoleId);


    if (existingMapping != null)
    {
        if (existingMapping.IsActive)
        {
            TempData["Error"] =
                "User already has this role.";

            return RedirectToAction("UserAccess");
        }

        existingMapping.IsActive = true;

        existingMapping.AssignedAt =
            DateTime.UtcNow;

        existingMapping.AssignedBy =
            long.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId)
                ? currentUserId
                : null;

        await UpsertUserRoleTable(request.UserId, request.RoleId, true, existingMapping.AssignedBy);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Role reactivated successfully.";

        return RedirectToAction("UserAccess");
    }


    var mapping = new UserRoleMapping
    {
        UserId = request.UserId,

        RoleId = request.RoleId,

        AssignedAt = DateTime.UtcNow,

        AssignedBy =
            long.TryParse(HttpContext.Session.GetString("UserId"), out var assignedBy)
                ? assignedBy
                : null,

        IsActive = true
    };

    _context.UserRoleMappings.Add(mapping);

    await UpsertUserRoleTable(request.UserId, request.RoleId, true, mapping.AssignedBy);

    await _context.SaveChangesAsync();

    TempData["Success"] =
        "Role assigned successfully.";

    return RedirectToAction("UserAccess");
}
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RemoveUserRole(long userId, long roleId)
{

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

    var activeRoleCount = _context.UserRoleMappings
        .Count(x => x.UserId == userId && x.IsActive);

    if (activeRoleCount <= 1)
    {
        TempData["Error"] = "At least one active role is required for every user.";
        return RedirectToAction("UserAccess");
    }


    mapping.IsActive = false;

    await UpsertUserRoleTable(userId, roleId, false, null);

    await _context.SaveChangesAsync();

    TempData["Success"] =
        "Role removed successfully.";

    return RedirectToAction("UserAccess");
}

private async Task UpsertUserRoleTable(long userId, long roleId, bool isActive, long? assignedBy)
{
    await _context.Database.ExecuteSqlInterpolatedAsync($@"
CREATE TABLE IF NOT EXISTS public.user_roles
(
    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id bigint NOT NULL,
    role_id bigint NOT NULL,
    assigned_by bigint,
    assigned_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    is_active boolean DEFAULT true
);

INSERT INTO public.user_roles (user_id, role_id, assigned_by, assigned_at, is_active)
VALUES ({userId}, {roleId}, {assignedBy}, CURRENT_TIMESTAMP, {isActive})
ON CONFLICT DO NOTHING;

UPDATE public.user_roles
SET is_active = {isActive},
    assigned_by = COALESCE({assignedBy}, assigned_by),
    assigned_at = CURRENT_TIMESTAMP
WHERE user_id = {userId}
  AND role_id = {roleId};");
}

public IActionResult DeleteUser(int id)
{
    var user = _context.Users.FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return RedirectToAction("Users");
    }


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

_context.DeletedUsers.Add(deletedUser);


user.is_deleted = true;
user.is_active = false;

user.UpdatedAt = DateTime.UtcNow;


_context.SaveChanges();

    return RedirectToAction("Users");
}


public IActionResult RevokeUser(int id)
{
    var user = _context.Users.FirstOrDefault(x => x.Id == id);

    if (user == null)
    {
        return RedirectToAction("Users");
    }

    user.is_deleted = false;
    user.is_active = true;


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

private string NormalizeCode(string? value)
{
    return (value ?? string.Empty)
        .Trim()
        .ToUpperInvariant()
        .Replace(" ", "_")
        .Replace("-", "_");
}

private static bool TryGetAdminPermission(
    string actionName,
    out string moduleCode,
    out string actionType)
{
    var map = new Dictionary<string, (string ModuleCode, string ActionType)>(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Dashboard)] = ("ADMIN", "VIEW"),
        [nameof(ExportDashboard)] = ("ADMIN", "VIEW"),
        [nameof(ActivityLogs)] = ("ADMIN", "VIEW"),
        [nameof(Versions)] = ("ADMIN", "VIEW"),
        [nameof(CreateVersion)] = ("ADMIN", "VIEW"),
        [nameof(CreateNextVersion)] = ("ADMIN", "VIEW"),
        [nameof(UpdateVersion)] = ("ADMIN", "VIEW"),
        [nameof(DeleteVersion)] = ("ADMIN", "VIEW"),
        [nameof(AccessManagement)] = ("ADMIN", "VIEW"),
        [nameof(Menus)] = ("ADMIN", "VIEW"),

        [nameof(Users)] = ("USER", "VIEW"),
        [nameof(UserDetails)] = ("USER", "VIEW"),
        [nameof(DisableUser)] = ("USER", "DISABLE"),
        [nameof(EnableUser)] = ("USER", "DISABLE"),
        [nameof(ToggleUserStatus)] = ("USER", "DISABLE"),
        [nameof(DeleteUser)] = ("USER", "DISABLE"),
        [nameof(RevokeUser)] = ("USER", "DISABLE"),
        [nameof(UserAccess)] = ("USER", "GRANT_ACCESS"),
        [nameof(AddUserRole)] = ("USER", "GRANT_ACCESS"),
        [nameof(RemoveUserRole)] = ("USER", "GRANT_ACCESS"),

        [nameof(Roles)] = ("ROLE", "VIEW"),
        [nameof(CreateRole)] = ("ROLE", "CREATE"),
        [nameof(UpdateRole)] = ("ROLE", "UPDATE"),

        [nameof(Permissions)] = ("PERMISSION", "VIEW"),
        [nameof(CreatePermission)] = ("PERMISSION", "CREATE"),
        [nameof(ToggleRolePermission)] = ("PERMISSION", "ASSIGN"),

        [nameof(ManageShows)] = ("SHOW", "VIEW"),
        [nameof(ContentManager)] = ("SHOW", "VIEW"),
        [nameof(SaveContent)] = ("SHOW", "UPDATE"),
        [nameof(DeleteContent)] = ("SHOW", "DELETE"),
        [nameof(SaveNewsChannel)] = ("SHOW", "UPDATE"),
        [nameof(DeleteNewsChannel)] = ("SHOW", "DELETE"),
        [nameof(CreateManagedShow)] = ("SHOW", "CREATE"),
        [nameof(UpdateManagedShow)] = ("SHOW", "UPDATE"),
        [nameof(DeleteManagedShow)] = ("SHOW", "DELETE"),

        [nameof(Bookings)] = ("BOOKING", "VIEW"),
        [nameof(Security)] = ("SCANNER", "VIEW"),
        [nameof(AcknowledgeSecurityAlerts)] = ("SCANNER", "VIEW"),
        [nameof(AddSecurityValidation)] = ("SCANNER", "VIEW"),
        [nameof(ClearSecurityAlert)] = ("SCANNER", "VIEW"),
        [nameof(BlockTicketFromSecurity)] = ("SCANNER", "VIEW"),
        [nameof(RegisterScannerDevice)] = ("SCANNER", "VIEW"),

        [nameof(Transactions)] = ("PAYMENT", "VIEW"),
        [nameof(TransactionDetails)] = ("PAYMENT", "VIEW"),

        [nameof(Refunds)] = ("REFUND", "VIEW"),
        [nameof(RefundDetails)] = ("REFUND", "VIEW"),
        [nameof(ApproveRefund)] = ("REFUND", "APPROVE"),
        [nameof(RejectRefund)] = ("REFUND", "REJECT"),
        [nameof(RetryRefund)] = ("REFUND", "RETRY"),
        [nameof(SaveRefundNotes)] = ("REFUND", "UPDATE"),
        [nameof(ExportRefunds)] = ("REFUND", "VIEW"),

        [nameof(Wallets)] = ("WALLET", "VIEW"),
        [nameof(SuspendWallet)] = ("WALLET", "UPDATE"),
        [nameof(ReactivateWallet)] = ("WALLET", "UPDATE"),
        [nameof(CouponUsage)] = ("COUPON", "VIEW"),
        [nameof(Notifications)] = ("NOTIFICATION", "VIEW")
    };

    if (map.TryGetValue(actionName, out var permission))
    {
        moduleCode = permission.ModuleCode;
        actionType = permission.ActionType;
        return true;
    }

    moduleCode = string.Empty;
    actionType = string.Empty;
    return false;
}

private async Task EnsureRbacInfrastructure()
{
    EnsurePermissionSeedData();
    EnsureDefaultRolePermissions();

    await _context.Database.ExecuteSqlRawAsync(@"
INSERT INTO public.user_role_mappings (user_id, role_id, assigned_by, assigned_at, is_active)
SELECT u.""Id"", r.id, NULL, CURRENT_TIMESTAMP, true
FROM public.""Users"" u
JOIN public.roles r ON r.role_code = 'AMAR_USER'
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.user_role_mappings existing
    WHERE existing.user_id = u.""Id""
);

CREATE OR REPLACE VIEW public.vw_user_access_matrix AS
SELECT
    u.""Id"" AS user_id,
    u.""Name"" AS user_name,
    u.""Email"" AS user_email,
    r.role_code,
    r.role_name,
    am.module_code,
    am.module_name,
    p.permission_code,
    p.permission_name,
    p.action_type,
    urm.assigned_at,
    urm.is_active
FROM public.""Users"" u
JOIN public.user_role_mappings urm ON u.""Id"" = urm.user_id
JOIN public.roles r ON urm.role_id = r.id
JOIN public.role_permissions rp ON r.id = rp.role_id
JOIN public.permissions p ON rp.permission_id = p.id
JOIN public.application_modules am ON p.module_id = am.id
WHERE urm.is_active = true
  AND r.is_active = true
  AND am.is_active = true;

CREATE OR REPLACE VIEW public.vw_user_application_menus AS
SELECT
    u.""Id"" AS user_id,
    u.""Name"" AS user_name,
    r.role_code,
    am.id AS menu_id,
    am.module_code AS menu_code,
    am.module_name AS menu_name,
    NULL::bigint AS parent_menu_id,
    NULL::varchar(255) AS parent_menu_name,
    am.route_path,
    am.icon_name,
    1 AS menu_level,
    am.display_order,
    true AS can_view,
    bool_or(p.action_type = 'CREATE') AS can_create,
    bool_or(p.action_type = 'UPDATE') AS can_update,
    bool_or(p.action_type = 'DELETE') AS can_delete
FROM public.""Users"" u
JOIN public.user_role_mappings urm ON u.""Id"" = urm.user_id
JOIN public.roles r ON urm.role_id = r.id
JOIN public.role_permissions rp ON r.id = rp.role_id
JOIN public.permissions p ON rp.permission_id = p.id
JOIN public.application_modules am ON p.module_id = am.id
WHERE urm.is_active = true
  AND r.is_active = true
  AND am.is_active = true
GROUP BY
    u.""Id"",
    u.""Name"",
    r.role_code,
    am.id,
    am.module_code,
    am.module_name,
    am.route_path,
    am.icon_name,
    am.display_order;");
}

private void EnsurePermissionSeedData()
{
    var now = DateTime.UtcNow;

    var roles = new[]
    {
        ("AMAR_SUPER_ADMIN", "Super Admin", "Full access to every application module, including developer tools."),
        ("AMAR_ADMIN", "Administrator", "Administrative access to operations, users, shows, bookings, payments, refunds, wallet, coupons, notifications, scanner, and analytics."),
        ("AMAR_DEVELOPER", "Developer", "Developer profile and developer-only editor access."),
        ("AMAR_USER", "User", "Default customer role for booking and profile workflows."),
        ("DUM_ADMIN", "dum_Admin", "Dashboard access with Developer Profile and My Profile.")
    };

    foreach (var roleSeed in roles)
    {
        var existingRole = _context.Roles.FirstOrDefault(x => x.RoleCode == roleSeed.Item1);
        if (existingRole == null)
        {
            _context.Roles.Add(new Role
            {
                RoleCode = roleSeed.Item1,
                RoleName = roleSeed.Item2,
                RoleDescription = roleSeed.Item3,
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existingRole.RoleName = roleSeed.Item2;
            existingRole.RoleDescription = roleSeed.Item3;
            existingRole.IsSystemRole = true;
            existingRole.IsActive = true;
            existingRole.UpdatedAt = now;
        }
    }

    _context.SaveChanges();

    var modules = new[]
    {
        ("ADMIN", "Admin Dashboard", "/Admin/Dashboard", 10),
        ("USER", "Users and Profiles", "/Admin/Users", 20),
        ("ROLE", "Roles", "/Admin/Roles", 30),
        ("PERMISSION", "Permissions", "/Admin/Roles", 40),
        ("SHOW", "Manage Shows", "/Admin/ManageShows", 50),
        ("BOOKING", "Bookings and Tickets", "/Admin/Bookings", 60),
        ("PAYMENT", "Payments", "/Admin/Transactions", 70),
        ("REFUND", "Refunds", "/Admin/Refunds", 80),
        ("WALLET", "Wallets", "/Admin/Wallets", 90),
        ("COUPON", "Coupons", "/Admin/CouponUsage", 100),
        ("NOTIFICATION", "Notifications", "/Admin/Notifications", 110),
        ("ANALYTICS", "Analytics", "/Admin/Dashboard", 120),
        ("SUPPORT", "Support", "/Admin/UserAccess", 130),
        ("SCANNER", "Ticket Scanner", "/Admin/Security", 140),
        ("DEVELOPER", "Developer Editor", "/Developer/Profile", 150)
    };

    foreach (var module in modules)
    {
        if (!_context.ApplicationModules.Any(x => x.ModuleCode == module.Item1))
        {
            _context.ApplicationModules.Add(new ApplicationModule
            {
                ModuleCode = module.Item1,
                ModuleName = module.Item2,
                RoutePath = module.Item3,
                IconName = module.Item1.ToLowerInvariant(),
                DisplayOrder = module.Item4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    foreach (var module in modules)
    {
        var existing =
            _context.ApplicationModules
            .FirstOrDefault(x => x.ModuleCode == module.Item1);

        if (existing == null)
        {
            continue;
        }

        existing.RoutePath = module.Item3;
        existing.DisplayOrder = module.Item4;
    }

    _context.SaveChanges();

    var permissions = new[]
    {
        ("ADMIN", "VIEW"), ("USER", "VIEW"), ("USER", "UPDATE"), ("USER", "DISABLE"), ("USER", "GRANT_ACCESS"),
        ("ROLE", "VIEW"), ("ROLE", "CREATE"), ("ROLE", "UPDATE"), ("ROLE", "DELETE"),
        ("PERMISSION", "VIEW"), ("PERMISSION", "CREATE"), ("PERMISSION", "ASSIGN"),
        ("SHOW", "VIEW"), ("SHOW", "CREATE"), ("SHOW", "UPDATE"), ("SHOW", "DELETE"),
        ("BOOKING", "VIEW"), ("BOOKING", "PRINT"), ("BOOKING", "CANCEL"),
        ("PAYMENT", "VIEW"), ("PAYMENT", "REFUND"),
        ("REFUND", "VIEW"), ("REFUND", "APPROVE"), ("REFUND", "REJECT"), ("REFUND", "RETRY"), ("REFUND", "UPDATE"),
        ("WALLET", "VIEW"), ("WALLET", "UPDATE"),
        ("COUPON", "VIEW"), ("COUPON", "CREATE"), ("COUPON", "UPDATE"), ("COUPON", "DELETE"),
        ("NOTIFICATION", "VIEW"), ("NOTIFICATION", "UPDATE"),
        ("ANALYTICS", "VIEW"), ("SUPPORT", "VIEW"), ("SCANNER", "VIEW"), ("SCANNER", "VALIDATE"), ("DEVELOPER", "EDIT")
    };

    var moduleLookup = _context.ApplicationModules.ToDictionary(x => x.ModuleCode, x => x.Id);

    foreach (var permission in permissions)
    {
        var code = $"{permission.Item1}_{permission.Item2}";
        if (!_context.Permissions.Any(x => x.PermissionCode == code) && moduleLookup.TryGetValue(permission.Item1, out var moduleId))
        {
            _context.Permissions.Add(new Permission
            {
                ModuleId = moduleId,
                PermissionCode = code,
                PermissionName = $"{permission.Item1} {permission.Item2}".Replace("_", " "),
                ActionType = permission.Item2,
                Description = $"Allows {permission.Item2.ToLowerInvariant()} access for {permission.Item1}.",
                CreatedAt = now
            });
        }
    }

	_context.SaveChanges();

    var walletUpdatePermission =
        _context.Permissions.FirstOrDefault(x => x.PermissionCode == "WALLET_UPDATE");

    if(walletUpdatePermission != null)
    {
        var walletAdminRoles =
            _context.Roles
            .Where(x =>
                x.RoleCode == "AMAR_SUPER_ADMIN" ||
                x.RoleCode == "AMAR_ADMIN")
            .ToList();

        foreach(var role in walletAdminRoles)
        {
            if(!_context.RolePermissions.Any(x => x.RoleId == role.Id && x.PermissionId == walletUpdatePermission.Id))
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = walletUpdatePermission.Id,
                    GrantedAt = now
                });
            }
        }

        _context.SaveChanges();
    }
}

private void EnsureDefaultRolePermissions()
{
    long? grantedBy = null;
    if (long.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId))
    {
        grantedBy = currentUserId;
    }

    _context.Database.ExecuteSqlInterpolated($@"
DELETE FROM public.role_permissions
WHERE role_id IN
(
    SELECT id
    FROM public.roles
    WHERE role_code IN ('AMAR_SUPER_ADMIN', 'AMAR_ADMIN', 'AMAR_DEVELOPER', 'AMAR_USER', 'DUM_ADMIN')
);

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, {grantedBy}, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON true
WHERE r.role_code = 'AMAR_SUPER_ADMIN'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, {grantedBy}, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON p.permission_code NOT LIKE 'DEVELOPER_%'
WHERE r.role_code = 'AMAR_ADMIN'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, {grantedBy}, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON p.permission_code = 'DEVELOPER_EDIT'
WHERE r.role_code = 'AMAR_DEVELOPER'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions
(
    role_id,
    permission_id,
    granted_by,
    granted_at
)
SELECT r.id, p.id, {grantedBy}, CURRENT_TIMESTAMP
FROM public.roles r
JOIN public.permissions p ON p.permission_code IN
(
    'ADMIN_VIEW',
    'USER_VIEW',
    'BOOKING_VIEW',
    'SCANNER_VIEW',
    'PAYMENT_VIEW',
    'REFUND_VIEW',
    'COUPON_VIEW',
    'WALLET_VIEW',
    'NOTIFICATION_VIEW',
    'DEVELOPER_EDIT'
)
WHERE r.role_code = 'DUM_ADMIN'
ON CONFLICT DO NOTHING;");
}

private static ShowSchedule CreateSchedule(
    ManageShowCreateViewModel request,
    string type,
    int duration,
    DateTime startTime)
{
    var utcStart = DateTime.SpecifyKind(startTime, DateTimeKind.Local).ToUniversalTime();

    return new ShowSchedule
    {
        LocationId = request.LocationId,
        ScreenId = request.ScreenId,
        StartTime = utcStart,
        EndTime = utcStart.AddMinutes(duration),
        ShowDay = GetScheduleDayName(startTime),
        Type = type
    };
}

private static string GetScheduleDayName(DateTime showDate)
{
    return showDate.ToString("dddd", CultureInfo.InvariantCulture);
}

private static List<DateTime> BuildManagedShowStartTimes(ManageShowCreateViewModel request)
{
    var starts = new List<DateTime> { request.StartTime };

    if (!string.IsNullOrWhiteSpace(request.AdditionalStartTimes))
    {
        var parts = request.AdditionalStartTimes.Split(
            new[] { ',', '\n', '\r', ';' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (DateTime.TryParse(part, out var parsed))
            {
                starts.Add(parsed);
            }
        }
    }

    return starts
        .Distinct()
        .OrderBy(x => x)
        .ToList();
}

private async Task<long> ResolveManagedScreenId(long venueId, long screenId)
{
    if (venueId > 0)
    {
        var selectedScreen = await _context.Screens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == screenId && x.VenueId == venueId && x.IsActive);

        if (selectedScreen != null)
        {
            return selectedScreen.Id;
        }

        return await _context.Screens
            .AsNoTracking()
            .Where(x => x.VenueId == venueId && x.IsActive)
            .OrderBy(x => x.ScreenName)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
    }

    return await _context.Screens
        .AsNoTracking()
        .Where(x => x.Id == screenId && x.IsActive)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();
}

private ManagedShowMetadata NormalizeManagedShowMetadata(
    string? secondaryName,
    string? cast,
    string? description,
    string? posterUrl,
    string? images,
    string? trailerUrl,
    decimal? imdbRating)
{
    return new ManagedShowMetadata
    {
        SecondaryName = CleanText(secondaryName),
        Cast = CleanText(cast),
        Description = CleanText(description),
        PosterUrl = CleanText(posterUrl),
        Images = CleanText(images),
        TrailerUrl = CleanText(trailerUrl),
        ImdbRating = NormalizeImdbRating(imdbRating)
    };
}

private decimal? NormalizeImdbRating(decimal? imdbRating)
{
    if (!imdbRating.HasValue && Request.HasFormContentType)
    {
        var rawRating = Request.Form["ImdbRating"].ToString();
        if (decimal.TryParse(rawRating, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantRating) ||
            decimal.TryParse(rawRating, NumberStyles.Number, CultureInfo.CurrentCulture, out invariantRating))
        {
            imdbRating = invariantRating;
        }
    }

    if (!imdbRating.HasValue)
    {
        return null;
    }

    return Math.Clamp(imdbRating.Value, 0m, 10m);
}

private static string? CleanText(string? value)
{
    var cleaned = value?.Trim();
    return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
}

private sealed class ManagedShowMetadata
{
    public string? SecondaryName { get; init; }
    public string? Cast { get; init; }
    public string? Description { get; init; }
    public string? PosterUrl { get; init; }
    public string? Images { get; init; }
    public string? TrailerUrl { get; init; }
    public decimal? ImdbRating { get; init; }
}

private async Task<List<AdminNotificationActionItem>> BuildAdminNotificationActions()
{
    var events = new List<AdminNotificationActionItem>();
    var refundByBookingRef = new Dictionary<string, VwRefundSummary>(StringComparer.OrdinalIgnoreCase);

    try
    {
        var refundLookup = await _context.VwRefundSummaries
            .AsNoTracking()
            .Where(x => !string.IsNullOrWhiteSpace(x.BookingRef))
            .OrderByDescending(x => x.RequestedAt ?? x.CreatedAt)
            .Take(100)
            .ToListAsync();

        refundByBookingRef = refundLookup
            .GroupBy(x => x.BookingRef!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        var activeRefunds = refundLookup
            .Where(x =>
                x.RefundStatus == "PENDING" ||
                x.RefundStatus == "FAILED")
            .Take(40)
            .ToList();

        if (activeRefunds.Count < 40)
        {
            var missingRefunds = await _context.VwRefundSummaries
            .AsNoTracking()
            .Where(x =>
                x.RefundStatus == "PENDING" ||
                x.RefundStatus == "FAILED")
            .OrderByDescending(x => x.RequestedAt ?? x.CreatedAt)
            .Take(40)
            .ToListAsync();

            activeRefunds = missingRefunds;
        }

        events.AddRange(activeRefunds.Select(x => new AdminNotificationActionItem
        {
            Id = $"refund-{x.RefundId}",
            Time = x.RequestedAt ?? x.CreatedAt,
            Category = "REFUND",
            Title = string.IsNullOrWhiteSpace(x.RefundRef) ? "Refund approval pending" : x.RefundRef,
            Status = string.IsNullOrWhiteSpace(x.RefundStatus) ? "PENDING" : x.RefundStatus,
            Priority = x.RefundStatus == "FAILED" ? "HIGH" : "MEDIUM",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"Booking {NullText(x.BookingRef)} | Transaction {NullText(x.TransactionRef)} | Amount {CurrencyFormatter.FormatRupees(x.RefundAmount)} | Reason {NullText(x.RefundReason)} | Method {NullText(x.RefundMethod)} | Gateway {NullText(x.GatewayRefundId)} | Failure {NullText(x.FailureReason)}",
            ActionText = x.RefundStatus == "FAILED" ? "Retry" : "Review",
            ActionUrl = $"/Admin/Refunds?highlight=refund-{x.RefundId}",
            RequiresAction = true
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification action refund source failed: {ex.Message}");
    }

    try
    {
        var cancelledBookings = await _context.VwBookingCompleteDetails
            .AsNoTracking()
            .Where(x => x.BookingStatus == "CANCELLED")
            .OrderByDescending(x => x.CancelledAt ?? x.BookedAt)
            .Take(25)
            .ToListAsync();

        events.AddRange(cancelledBookings
        .Where(x =>
            string.IsNullOrWhiteSpace(x.BookingRef) ||
            !refundByBookingRef.ContainsKey(x.BookingRef.Trim()))
        .Select(x =>
        {
            VwRefundSummary? refund = null;

            if (!string.IsNullOrWhiteSpace(x.BookingRef))
            {
                refundByBookingRef.TryGetValue(x.BookingRef.Trim(), out refund);
            }

            return new AdminNotificationActionItem
            {
                Id = $"booking-{x.BookingId}",
                Time = x.CancelledAt ?? x.BookedAt,
                Category = "BOOKING",
                Title = string.IsNullOrWhiteSpace(x.BookingRef) ? "Ticket cancelled" : x.BookingRef,
                Status = refund?.RefundStatus ?? "CANCELLED",
                Priority = refund == null || refund.RefundStatus == "FAILED" ? "HIGH" : "MEDIUM",
                UserName = x.UserName ?? string.Empty,
                UserEmail = x.UserEmail ?? string.Empty,
                Detail = $"{NullText(x.ShowTitle)} | Seats {NullText(x.SeatNumbers)} | Payment {NullText(x.PaymentStatus)} | Transaction {NullText(x.TransactionStatus)} | Refund {NullText(refund?.RefundStatus)} {CurrencyFormatter.FormatRupees(refund?.RefundAmount)} | Reason {NullText(refund?.RefundReason)}",
                ActionText = refund == null ? "Open" : "Review",
                ActionUrl = refund == null
                    ? $"/Admin/Bookings?highlight=booking-{x.BookingId}"
                    : $"/Admin/Refunds?highlight=refund-{refund.RefundId}",
                RequiresAction = true
            };
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification action booking source failed: {ex.Message}");
    }

    try
    {
        await EnsureWalletAdminSchema();

        var suspendedWallets = await _context.VwWalletSummaries
            .AsNoTracking()
            .Where(x => x.WalletStatus == "SUSPENDED")
            .OrderByDescending(x => x.SuspendedAt ?? x.LastTransactionAt)
            .Take(25)
            .ToListAsync();

        events.AddRange(suspendedWallets.Select(x => new AdminNotificationActionItem
        {
            Id = $"wallet-{x.WalletId}",
            Time = x.SuspendedAt ?? x.LastTransactionAt ?? DateTime.MinValue,
            Category = "WALLET",
            Title = "Wallet account suspended",
            Status = "SUSPENDED",
            Priority = "HIGH",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"Reason {NullText(x.SuspensionReason)} | Balance {CurrencyFormatter.FormatRupees(x.WalletBalance)} | Blocked {CurrencyFormatter.FormatRupees(x.BlockedBalance)} | Suspended by {NullText(x.SuspendedBy)}",
            ActionText = "Review Wallet",
            ActionUrl = $"/Admin/Wallets?highlight=wallet-{x.WalletId}",
            RequiresAction = true
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification action wallet source failed: {ex.Message}");
    }

    try
    {
        var failedTransactions = await _context.VwBookingTransactionSummaries
            .AsNoTracking()
            .Where(x => x.IsPaymentError == 1 || x.TransactionStatus == "FAILED" || x.TransactionStatus == "ERROR")
            .OrderByDescending(x => x.BookingCreatedAt)
            .Take(25)
            .ToListAsync();

        events.AddRange(failedTransactions.Select(x => new AdminNotificationActionItem
        {
            Id = x.TransactionId.HasValue ? $"transaction-{x.TransactionId.Value}" : $"booking-{x.BookingId}",
            Time = x.BookingCreatedAt,
            Category = "PAYMENT",
            Title = string.IsNullOrWhiteSpace(x.TransactionRef) ? "Payment failed" : x.TransactionRef,
            Status = string.IsNullOrWhiteSpace(x.TransactionStatus) ? "FAILED" : x.TransactionStatus,
            Priority = "HIGH",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"{NullText(x.BookingRef)} | {NullText(x.ShowTitle)} | {NullText(x.PaymentMethod)} | {NullText(x.FailureReason)}",
            ActionText = "Open",
            ActionUrl = x.TransactionId.HasValue
                ? $"/Admin/Transactions?highlight=transaction-{x.TransactionId.Value}"
                : $"/Admin/Bookings?highlight=booking-{x.BookingId}",
            RequiresAction = true
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification action payment source failed: {ex.Message}");
    }

    try
    {
        var ticketIssues = await _context.VwTicketValidationSummaries
            .AsNoTracking()
            .Where(x => x.IsSecurityIssue == 1)
            .OrderByDescending(x => x.ValidatedAt)
            .Take(20)
            .ToListAsync();

        events.AddRange(ticketIssues.Select(x => new AdminNotificationActionItem
        {
            Id = $"ticket-{x.TicketId}",
            Time = x.ValidatedAt ?? DateTime.MinValue,
            Category = "SECURITY",
            Title = string.IsNullOrWhiteSpace(x.TicketNumber) ? "Ticket validation issue" : x.TicketNumber,
            Status = string.IsNullOrWhiteSpace(x.ValidationResult) ? "ISSUE" : x.ValidationResult,
            Priority = "HIGH",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"{NullText(x.BookingRef)} | {NullText(x.ShowTitle)} | {NullText(x.ValidationMessage)}",
            ActionText = "Open",
            ActionUrl = "/Admin/Security",
            RequiresAction = true
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification action security source failed: {ex.Message}");
    }

    return events
        .Where(x => x.RequiresAction)
        .GroupBy(x => GetAdminNotificationDedupKey(x), StringComparer.OrdinalIgnoreCase)
        .Select(x => x.First())
        .OrderByDescending(x => x.RequiresAction)
        .ThenByDescending(x => x.Priority == "HIGH")
        .ThenByDescending(x => x.Time)
        .Take(50)
        .ToList();
}

private async Task<List<AdminNotificationActionItem>> BuildAdminNotificationArchiveActions()
{
    var events =
        new List<AdminNotificationActionItem>();

    try
    {
        var completedRefunds =
            await _context.VwRefundSummaries
            .AsNoTracking()
            .Where(x =>
                x.RefundStatus == "SUCCESS" ||
                x.RefundStatus == "REJECTED" ||
                x.RefundStatus == "PROCESSING" ||
                x.workflow_action == "RETRY INITIATED BY ADMIN")
            .OrderByDescending(x =>
                x.rejected_at ??
                x.approved_at ??
                x.retried_at ??
                x.ProcessedAt ??
                x.UpdatedAt ??
                x.RequestedAt ??
                x.CreatedAt)
            .Take(50)
            .ToListAsync();

        events.AddRange(completedRefunds.Select(x =>
        {
            var actionTime =
                x.rejected_at ??
                x.approved_at ??
                x.retried_at ??
                x.ProcessedAt ??
                x.UpdatedAt ??
                x.RequestedAt ??
                x.CreatedAt;

            var status =
                string.IsNullOrWhiteSpace(x.RefundStatus)
                    ? "COMPLETED"
                    : x.RefundStatus;

            var owner =
                x.RefundStatus == "REJECTED"
                    ? x.rejected_by
                    : x.RefundStatus == "SUCCESS"
                        ? x.approved_by
                        : x.retried_by;

            return new AdminNotificationActionItem
            {
                Id = $"archive-refund-{x.RefundId}",
                Time = actionTime,
                Category = "REFUND",
                Title = string.IsNullOrWhiteSpace(x.RefundRef) ? "Refund action completed" : x.RefundRef,
                Status = status,
                Priority = x.RefundStatus == "REJECTED" ? "MEDIUM" : "LOW",
                UserName = x.UserName ?? string.Empty,
                UserEmail = x.UserEmail ?? string.Empty,
                Detail = $"Booking {NullText(x.BookingRef)} | Transaction {NullText(x.TransactionRef)} | Amount {CurrencyFormatter.FormatRupees(x.RefundAmount)} | Action {NullText(x.workflow_action)} | By {NullText(owner)} | Notes {NullText(x.admin_notes)}",
                ActionText = "Open",
                ActionUrl = $"/Admin/RefundDetails/{x.RefundId}",
                RequiresAction = false
            };
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification archive refund source failed: {ex.Message}");
    }

    try
    {
        var deliveredNotifications =
            await _context.VwNotificationCenters
            .AsNoTracking()
            .Where(x =>
                x.Status == "SENT" ||
                x.Status == "DELIVERED" ||
                x.Status == "READ" ||
                x.Status == "SUCCESS")
            .OrderByDescending(x => x.CreatedAt)
            .Take(30)
            .ToListAsync();

        events.AddRange(deliveredNotifications.Select(x => new AdminNotificationActionItem
        {
            Id = $"archive-notification-{x.NotificationId}",
            Time = x.DeliveredAt ?? x.SentAt ?? x.CreatedAt,
            Category = "NOTIFICATION",
            Title = string.IsNullOrWhiteSpace(x.Title) ? "Notification completed" : x.Title,
            Status = string.IsNullOrWhiteSpace(x.Status) ? "SENT" : x.Status,
            Priority = "LOW",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"Type {NullText(x.NotificationType)} | Message {NullText(x.Message)} | Sent {FormatDateText(x.SentAt)} | Delivered {FormatDateText(x.DeliveredAt)} | Read {FormatDateText(x.ReadAt)}",
            ActionText = "Open",
            ActionUrl = $"/Admin/Notifications?highlight=notification-{x.NotificationId}",
            RequiresAction = false
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification archive delivery source failed: {ex.Message}");
    }

    try
    {
        var failedNotificationDeliveries =
            await _context.VwNotificationCenters
            .AsNoTracking()
            .Where(x =>
                x.IsError == 1 ||
                x.Status == "FAILED" ||
                x.Status == "ERROR")
            .OrderByDescending(x => x.CreatedAt)
            .Take(30)
            .ToListAsync();

        events.AddRange(failedNotificationDeliveries.Select(x => new AdminNotificationActionItem
        {
            Id = $"archive-notification-failed-{x.NotificationId}",
            Time = x.CreatedAt,
            Category = "NOTIFICATION",
            Title = string.IsNullOrWhiteSpace(x.Title) ? "Notification delivery issue" : x.Title,
            Status = string.IsNullOrWhiteSpace(x.Status) ? "FAILED" : x.Status,
            Priority = "MEDIUM",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"Type {NullText(x.NotificationType)} | Template {NullText(x.TemplateCode)} - {NullText(x.TemplateName)} | Message {NullText(x.Message)} | Retries {x.RetryCount} | Sent {FormatDateText(x.SentAt)} | Delivered {FormatDateText(x.DeliveredAt)} | Read {FormatDateText(x.ReadAt)} | Failure {NullText(x.FailureReason)}",
            ActionText = "Review Delivery",
            ActionUrl = $"/Admin/Notifications?highlight=notification-{x.NotificationId}",
            RequiresAction = false
        }));
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Notification archive failed delivery source failed: {ex.Message}");
    }

    return events
        .GroupBy(x => GetAdminNotificationDedupKey(x), StringComparer.OrdinalIgnoreCase)
        .Select(x => x.First())
        .OrderByDescending(x => x.Time)
        .Take(50)
        .ToList();
}

private async Task<int> GetAdminNotificationCount()
{
    var actionItems =
        await BuildAdminNotificationActions();

    return actionItems.Count(x => x.RequiresAction);
}

private static async Task<int> CountAdminNotifications(Func<Task<int>> count)
{
    try
    {
        return await count();
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Admin notification count source failed: {ex.Message}");
        return 0;
    }
}

private static string GetAdminNotificationDedupKey(AdminNotificationActionItem item)
{
    if (!string.IsNullOrWhiteSpace(item.ActionUrl))
    {
        return item.ActionUrl.Trim();
    }

    if (!string.IsNullOrWhiteSpace(item.Title))
    {
        return $"{item.Category}|{item.Title.Trim()}";
    }

    return $"{item.Category}|{item.Id}";
}

private static string NullText(string? value)
{
    return string.IsNullOrWhiteSpace(value) ? "NA" : value.Trim();
}

private static string FormatDateText(DateTime? value)
{
    return value.HasValue
        ? value.Value.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
        : "NA";
}

private static string NormalizeSecurityStatus(string? value)
{
    var status = (value ?? string.Empty).Trim().ToUpperInvariant();

    return status switch
    {
        "SCANNED" or "BLOCKED" or "EXPIRED" or "CANCELLED" => status,
        _ => "SCANNED"
    };
}

private static string NormalizeSecurityResult(string? value)
{
    var result = (value ?? string.Empty).Trim().ToUpperInvariant();

    return result switch
    {
        "SUCCESS" or "INVALID_QR" or "DUPLICATE_SCAN" or "EXPIRED_TICKET" or "CANCELLED_TICKET" or "ADMIN_BLOCKED" => result,
        _ => "SUCCESS"
    };
}

private static string AppendAdminRemark(string? existing, string note)
{
    var stamp = DateTime.Now.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
    var addition = $"{stamp}: {note}";

    return string.IsNullOrWhiteSpace(existing)
        ? addition
        : $"{existing.Trim()} | {addition}";
}

private async Task TouchScannerDevice(string deviceCode, string? gateName, DateTime activeAt)
{
    deviceCode = string.IsNullOrWhiteSpace(deviceCode)
        ? "ADMIN-CONSOLE"
        : deviceCode.Trim().ToUpperInvariant();

    var device = await _context.ScannerDevices
        .FirstOrDefaultAsync(x => x.DeviceCode == deviceCode);

    if (device == null)
    {
        _context.ScannerDevices.Add(new ScannerDevice
        {
            DeviceCode = deviceCode,
            DeviceName = deviceCode,
            GateName = string.IsNullOrWhiteSpace(gateName) ? "Admin Gate" : gateName.Trim(),
            DeviceStatus = "ACTIVE",
            LastActiveAt = activeAt,
            CreatedAt = activeAt
        });

        return;
    }

    device.GateName = string.IsNullOrWhiteSpace(gateName) ? device.GateName : gateName.Trim();
    device.DeviceStatus = "ACTIVE";
    device.LastActiveAt = activeAt;
}

private async Task EnsureSecurityInfrastructure()
{
    await _context.Database.ExecuteSqlRawAsync(@"
CREATE TABLE IF NOT EXISTS public.ticket_validation_logs (
    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    ticket_id bigint NOT NULL,
    booking_id bigint,
    user_id bigint,
    scanned_qr_token uuid,
    validation_status varchar(50),
    validation_result varchar(100),
    gate_name varchar(100),
    device_id varchar(255),
    scanner_user varchar(255),
    scanner_ip varchar(45),
    latitude numeric(10,7),
    longitude numeric(10,7),
    remarks text,
    metadata jsonb,
    scanned_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS public.scanner_devices (
    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    device_code varchar(100) NOT NULL UNIQUE,
    device_name varchar(255),
    venue_id bigint,
    screen_id bigint,
    gate_name varchar(100),
    device_status varchar(50) DEFAULT 'ACTIVE',
    last_active_at timestamp without time zone,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE public.tickets ADD COLUMN IF NOT EXISTS qr_token uuid DEFAULT gen_random_uuid();
ALTER TABLE public.tickets ADD COLUMN IF NOT EXISTS validation_status varchar(50) DEFAULT 'NOT_SCANNED';
ALTER TABLE public.tickets ADD COLUMN IF NOT EXISTS validation_count integer DEFAULT 0;
ALTER TABLE public.tickets ADD COLUMN IF NOT EXISTS last_scanned_at timestamp without time zone;
ALTER TABLE public.tickets ADD COLUMN IF NOT EXISTS last_scanned_gate varchar(100);
ALTER TABLE public.tickets ADD COLUMN IF NOT EXISTS security_hash text;

CREATE INDEX IF NOT EXISTS idx_ticket_validation_status ON public.tickets (validation_status);
CREATE INDEX IF NOT EXISTS idx_validation_ticket ON public.ticket_validation_logs (ticket_id);
CREATE INDEX IF NOT EXISTS idx_validation_booking ON public.ticket_validation_logs (booking_id);
CREATE INDEX IF NOT EXISTS idx_validation_scanned_at ON public.ticket_validation_logs (scanned_at);

CREATE OR REPLACE VIEW public.vw_ticket_validation_summary AS
SELECT
    tvl.id AS validation_log_id,
    t.id AS ticket_id,
    t.ticket_number,
    b.booking_ref,
    u.""Name"" AS user_name,
    u.""Email"" AS user_email,
    tvl.validation_status,
    tvl.validation_result,
    tvl.gate_name,
    tvl.device_id,
    tvl.scanner_user,
    tvl.scanned_at,
    COALESCE(t.validation_count, 0) AS validation_count,
    t.last_scanned_at,
    CASE
        WHEN COALESCE(tvl.validation_result, '') = 'SUCCESS'
            AND COALESCE(tvl.validation_status, '') <> 'BLOCKED'
            THEN 0
        ELSE 1
    END AS is_security_issue
FROM public.ticket_validation_logs tvl
JOIN public.tickets t ON tvl.ticket_id = t.id
LEFT JOIN public.bookings b ON COALESCE(tvl.booking_id, t.booking_id) = b.id
LEFT JOIN public.""Users"" u ON COALESCE(tvl.user_id, b.user_id) = u.""Id"";
");
}

private async Task EnsureAdminShowInfrastructure()
{
    await _context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""Description"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""PosterUrl"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""Images"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""TrailerUrl"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""ImdbRating"" numeric(3,1);
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""Description"" text;
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""PosterUrl"" text;
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""Images"" text;
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""TrailerUrl"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""Description"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""PosterUrl"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""Images"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""TrailerUrl"" text;
ALTER TABLE public.""ShowSchedules"" ADD COLUMN IF NOT EXISTS ""ShowDay"" varchar(20);

UPDATE public.""ShowSchedules""
SET ""ShowDay"" = trim(to_char(""StartTime"", 'Day'))
WHERE ""ShowDay"" IS NULL OR trim(""ShowDay"") = '';

CREATE TABLE IF NOT EXISTS public.application_versions
(
    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    version_number varchar(50) NOT NULL,
    release_title varchar(255) NOT NULL,
    release_notes text,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by varchar(255),
    is_current boolean NOT NULL DEFAULT false
);

ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS version_number varchar(50) NOT NULL DEFAULT '1.0.0';
ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS release_title varchar(255) NOT NULL DEFAULT 'Application release';
ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS release_notes text;
ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS created_by varchar(255);
ALTER TABLE public.application_versions ADD COLUMN IF NOT EXISTS is_current boolean NOT NULL DEFAULT false;

DROP VIEW IF EXISTS public.vw_home_show_listing;

CREATE OR REPLACE VIEW public.vw_home_show_listing AS
SELECT
    s.""Id"" AS schedule_id,
    CASE
        WHEN s.""MovieId"" IS NOT NULL THEN 'Movie'
        WHEN s.""StandupShowId"" IS NOT NULL THEN 'Standup'
        WHEN s.""LiveStreamId"" IS NOT NULL THEN 'Live'
        ELSE COALESCE(NULLIF(s.""Type"", ''), 'Movie')
    END AS show_type,
    COALESCE(s.""MovieId"", s.""StandupShowId"", s.""LiveStreamId"", 0) AS show_id,
    COALESCE(m.""Title"", st.""Title"", ls.""Title"", 'Untitled Show') AS title,
    COALESCE(m.""Description"", st.""Description"", ls.""Description"",
        CASE
            WHEN m.""Id"" IS NOT NULL THEN concat_ws(' | ', NULLIF(m.""Director"", ''), NULLIF(m.""Producer"", ''), NULLIF(m.""Cast"", ''))
            WHEN st.""Id"" IS NOT NULL THEN 'Comedian: ' || st.""Comedian""
            WHEN ls.""Id"" IS NOT NULL THEN 'Host: ' || ls.""Host""
            ELSE ''
        END) AS ""Description"",
    COALESCE(m.""PosterUrl"", st.""PosterUrl"", ls.""PosterUrl"") AS ""PosterUrl"",
    COALESCE(m.""Images"", st.""Images"", ls.""Images"") AS ""Images"",
    COALESCE(m.""TrailerUrl"", st.""TrailerUrl"", ls.""TrailerUrl"") AS ""TrailerUrl"",
    COALESCE(m.""Director"", st.""Comedian"", ls.""Host"") AS director,
    m.""Cast"" AS cast,
    m.""ImdbRating"" AS imdb_rating,
    v.venue_name,
    sc.screen_name,
    s.""StartTime"" AS start_time,
    s.""EndTime"" AS end_time,
    COALESCE(l.""Area"", '') AS location,
    COALESCE(l.""State"", '') AS state,
    COALESCE(l.""Country"", '') AS country
FROM public.""ShowSchedules"" s
LEFT JOIN public.""Movies"" m ON s.""MovieId"" = m.""Id""
LEFT JOIN public.""StandupShows"" st ON s.""StandupShowId"" = st.""Id""
LEFT JOIN public.""LiveStreams"" ls ON s.""LiveStreamId"" = ls.""Id""
LEFT JOIN public.""Locations"" l ON s.""LocationId"" = l.""Id""
LEFT JOIN public.screens sc ON s.screen_id = sc.id
LEFT JOIN public.venues v ON sc.venue_id = v.id;

DROP VIEW IF EXISTS public.vw_coupon_usage_admin;

CREATE OR REPLACE VIEW public.vw_coupon_usage_admin AS
SELECT
    cu.id AS usage_id,
    cu.coupon_id,
    cu.coupon_code,
    cu.booking_id,
    b.booking_ref,
    cu.transaction_id,
    t.transaction_ref,
    cu.user_id,
    u.""Name"" AS user_name,
    u.""Email"" AS user_email,
    COALESCE(m.""Title"", st.""Title"", ls.""Title"", 'Untitled Show') AS show_name,
    s.""Type"" AS show_type,
    s.""StartTime"" AS show_time,
    cu.original_amount,
    cu.discount_amount,
    cu.final_amount,
    cu.usage_status,
    cu.used_at
FROM public.coupon_usage cu
LEFT JOIN public.bookings b ON cu.booking_id = b.id
LEFT JOIN public.transactions t ON cu.transaction_id = t.id
LEFT JOIN public.""Users"" u ON cu.user_id = u.""Id""
LEFT JOIN public.""ShowSchedules"" s ON b.schedule_id = s.""Id""
LEFT JOIN public.""Movies"" m ON s.""MovieId"" = m.""Id""
LEFT JOIN public.""StandupShows"" st ON s.""StandupShowId"" = st.""Id""
LEFT JOIN public.""LiveStreams"" ls ON s.""LiveStreamId"" = ls.""Id"";
");

    if (!await _context.Locations.AnyAsync())
    {
        var states = new[]
        {
            "Andhra Pradesh","Arunachal Pradesh","Assam","Bihar","Chhattisgarh","Goa","Gujarat","Haryana","Himachal Pradesh","Jharkhand",
            "Karnataka","Kerala","Madhya Pradesh","Maharashtra","Manipur","Meghalaya","Mizoram","Nagaland","Odisha","Punjab",
            "Rajasthan","Sikkim","Tamil Nadu","Telangana","Tripura","Uttar Pradesh","Uttarakhand","West Bengal","Delhi","Jammu and Kashmir"
        };

        var locations = new List<Location>();
        for (var i = 0; i < 500; i++)
        {
            var state = states[i % states.Length];
            locations.Add(new Location
            {
                Country = "India",
                State = state,
                Area = $"{state} City {i + 1:000}"
            });
        }

        _context.Locations.AddRange(locations);
        await _context.SaveChangesAsync();
    }

    if (!await _context.Venues.AnyAsync(x => x.IsActive))
    {
        var now = DateTime.UtcNow;
        _context.Venues.Add(new Venue
        {
            VenueCode = "ASB-IND-001",
            VenueName = "showTime Multiplex",
            VenueType = "Multiplex",
            Country = "India",
            State = "Karnataka",
            City = "Bengaluru",
            Address = "MG Road",
            TotalScreens = 6,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        });
        await _context.SaveChangesAsync();
    }

    var venues = await _context.Venues.Where(x => x.IsActive).ToListAsync();
    foreach (var venue in venues)
    {
        var existing = await _context.Screens.CountAsync(x => x.VenueId == venue.Id);
        for (var i = existing + 1; i <= 6; i++)
        {
            _context.Screens.Add(new Screen
            {
                VenueId = venue.Id,
                ScreenCode = $"{venue.VenueCode}-S{i}",
                ScreenName = $"Screen {i}",
                TotalSeats = 0,
                ScreenType = i % 2 == 0 ? "IMAX" : "Standard",
                AudioSystem = i % 2 == 0 ? "Dolby Atmos" : "Dolby 7.1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    await _context.SaveChangesAsync();
}

private async Task GenerateManagedSeats(
    int scheduleId,
    long screenId,
    int totalSeats,
    decimal silverPrice,
    decimal goldPrice,
    decimal premiumPrice)
{
    if (await _context.ScreenSeats.AnyAsync(x => x.ScheduleId == scheduleId))
    {
        return;
    }

    totalSeats = Math.Clamp(totalSeats, 1, 5000);
    silverPrice = silverPrice <= 0 ? 150 : silverPrice;
    goldPrice = goldPrice <= 0 ? 250 : goldPrice;
    premiumPrice = premiumPrice <= 0 ? 350 : premiumPrice;
    var seatsPerRow = 10;
    var rows = (int)Math.Ceiling(totalSeats / (double)seatsPerRow);
    var seats = new List<ScreenSeat>();

    for (var rowIndex = 0; rowIndex < rows; rowIndex++)
    {
        var rowName = ((char)('A' + rowIndex)).ToString();
        var seatsInRow = Math.Min(seatsPerRow, totalSeats - (rowIndex * seatsPerRow));
        var premiumStartRow = Math.Max(0, rows - 2);
        var goldStartRow = Math.Max(0, premiumStartRow - 3);

        for (var seatNumber = 1; seatNumber <= seatsInRow; seatNumber++)
        {
            var category = rowIndex >= premiumStartRow ? "Premium" : rowIndex >= goldStartRow ? "Gold" : "Silver";
            var price = category == "Premium" ? premiumPrice : category == "Gold" ? goldPrice : silverPrice;

            seats.Add(new ScreenSeat
            {
                ScheduleId = scheduleId,
                ScreenId = screenId,
                SeatRow = rowName,
                SeatNumber = seatNumber.ToString(),
                SeatCategory = category,
                SeatPrice = price,
                IsActive = true
            });
        }
    }

    _context.ScreenSeats.AddRange(seats);
    await _context.SaveChangesAsync();
}

    }
}
