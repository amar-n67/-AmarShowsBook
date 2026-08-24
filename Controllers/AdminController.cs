using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.Admin;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using AmarShowsBook.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;

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

        private readonly OtpDeliveryService _emailDeliveryService;

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

            if (TryGetAdminPermission(actionName, out var moduleCode, out var actionType))
            {
                await EnsureRbacInfrastructure();

                if (!RbacAuthorizationHelper.CanAccess(HttpContext, _rbacService, moduleCode, actionType))
                {
                    TempData["Error"] = "You do not have permission to access this admin feature.";
                    context.Result = actionName == nameof(Dashboard)
                        ? RedirectToAction("Index", "Home")
                        : RedirectToAction(nameof(Dashboard));
                    return;
                }
            }

            ViewData["AdminNotificationCount"] = await GetAdminNotificationCount();

            await next();
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

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var now = DateTime.UtcNow;

            var vm = new AdminDashboardViewModel
            {
                // ================= BOOKINGS =================

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

                // ================= PAYMENTS =================

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

                // ================= REFUNDS =================

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

                // ================= INVOICES =================

                InvoiceFailures =
                    _context.VwInvoiceSummaries
                        .Count(x => x.IsInvoiceError == 1),

                // ================= NOTIFICATIONS =================

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

                // ================= SECURITY =================

                TicketValidationIssues =
                    _context.VwTicketValidationSummaries
                        .Count(x => x.IsSecurityIssue == 1),

                ValidatedTickets =
                    _context.VwTicketValidationSummaries
                        .Count(x => x.IsSecurityIssue == 0),

                // ================= WALLET =================

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

                // ================= CONTENT / ACCESS =================

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
            var roles = _context.Roles
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToList();

            return View(roles);
        }

        // =====================================================
        // SECURITY PAGE
        // =====================================================

        public async Task<IActionResult> Security(int page = 1)
        {
            const int pageSize = 50;

            await EnsureSecurityInfrastructure();

            page = Math.Max(1, page);

            var query = _context.VwTicketValidationSummaries
                .AsNoTracking()
                .OrderByDescending(x => x.IsSecurityIssue)
                .ThenByDescending(x => x.LastScannedAt ?? x.ValidatedAt)
                .ThenByDescending(x => x.ValidationLogId);

            var totalCount = await query.CountAsync();

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
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

            roleCode = NormalizeCode(roleCode);
            roleName = (roleName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(roleCode) || string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Role code and role name are required.";
                return RedirectToAction(nameof(Roles));
            }

            if (await _context.Roles.AnyAsync(x => x.RoleCode == roleCode))
            {
                TempData["Error"] = "Role code already exists.";
                return RedirectToAction(nameof(Roles));
            }

          var now = DateTime.UtcNow;
            _context.Roles.Add(new Role
            {
                RoleCode = roleCode,
                RoleName = roleName,
                RoleDescription = roleDescription?.Trim(),
                IsSystemRole = false,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = HttpContext.Session.GetString("UserName") ?? "Admin",
                UpdatedBy = HttpContext.Session.GetString("UserName") ?? "Admin"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Role created successfully.";
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

            role.RoleName = (roleName ?? role.RoleName).Trim();
            role.RoleDescription = roleDescription?.Trim();
            role.IsActive = isActive;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = HttpContext.Session.GetString("UserName") ?? "Admin";

            await _context.SaveChangesAsync();
            TempData["Success"] = "Role updated successfully.";
            return RedirectToAction(nameof(Roles));
        }

        // =====================================================
        // PERMISSIONS PAGE
        // =====================================================

        // Human Comment:
        // Permission master screen

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
                    .OrderByDescending(x => x.LastTransactionAt);

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

    await using var dbTransaction =
        await _context.Database.BeginTransactionAsync();

    refund.refund_status = "REJECTED";

    refund.workflow_action =
        "REJECTED BY ADMIN - TICKET REMAINS ACTIVE";

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
        "Refund rejected by admin. Ticket cancellation request denied and ticket remains active.";

    await RestoreBookingAfterRefundRejection(refund);

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
                "Refund rejected by admin. Ticket remains active.",

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
    await dbTransaction.CommitAsync();

    // =====================================================
    // ACTIVITY LOG
    // =====================================================

    await _activityLogger.LogAsync(
        action: "REJECT_REFUND",
        module: "REFUND",
        entityType: "REFUND",
        description:
            $"Refund rejected and ticket kept active: {refund.refund_ref}",
        status: "SUCCESS",
        isError: 0
    );

    // =====================================================
    // NOTIFICATION LOG
    // =====================================================

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
        DateTime.UtcNow;

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
        "Thank you,\nAmarShowsBook";

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

public async Task<IActionResult> CouponUsage(int page = 1)
{
    await EnsureAdminShowInfrastructure();

    const int pageSize = 50;
    page = Math.Max(page, 1);

    var query = _context.VwCouponUsages
        .AsNoTracking()
        .OrderByDescending(x => x.UsedAt);

    var totalCount = await query.CountAsync();
    var rows = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    ViewBag.CurrentPage = page;
    ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
    ViewBag.TotalRecords = totalCount;

    return View(rows);
}

public async Task<IActionResult> Versions()
{
    await EnsureAdminShowInfrastructure();

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

    var versions = await _context.ApplicationVersions
        .AsNoTracking()
        .OrderByDescending(x => x.IsCurrent)
        .ThenByDescending(x => x.UpdatedAt)
        .ToListAsync();

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

    return $"{parts[0]}.{parts[1]}.{parts[2]}";
}

public async Task<IActionResult> ContentManager()
{
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

private static string NormalizeContentType(string? contentType)
{
    var value = (contentType ?? string.Empty).Trim().ToLowerInvariant();

    if (value.Contains("standup"))
    {
        return "Standup";
    }

    if (value.Contains("live"))
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
    // LOAD TRANSACTIONS AND BOOKINGS
    // =====================================================

    var transactions = _context.VwBookingTransactionSummaries
        .Where(x => x.UserId == id)
        .OrderByDescending(x => x.BookingCreatedAt)
        .ToList();

    var bookings = _context.VwBookingCompleteDetails
        .Where(x => x.UserId == id)
        .OrderByDescending(x => x.BookedAt)
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

        Bookings = bookings,

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
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddUserRole(UserRoleUpdateViewModel request)
{
    // =====================================================
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
    // VALIDATE ROLE EXISTS
    // =====================================================

    var roleExists =
        _context.Roles.Any(x =>
            x.Id == request.RoleId &&
            x.IsActive);

    if (!roleExists)
    {
        TempData["Error"] =
            "Role not found.";

        return RedirectToAction("UserAccess");
    }

    // =====================================================
    // CHECK EXISTING ROLE MAPPING
    // =====================================================

    var existingMapping =
        _context.UserRoleMappings
            .FirstOrDefault(x =>
                x.UserId == request.UserId &&
                x.RoleId == request.RoleId);

    // =====================================================
    // REACTIVATE OLD ROLE
    // =====================================================

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

    // =====================================================
    // CREATE NEW ROLE MAPPING
    // =====================================================

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

    if (long.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId) &&
        currentUserId == userId)
    {
        var activeRoleCount = _context.UserRoleMappings
            .Count(x => x.UserId == userId && x.IsActive);

        if (activeRoleCount <= 1)
        {
            TempData["Error"] = "You cannot remove your own last active role.";
            return RedirectToAction("UserAccess");
        }
    }

    // =====================================================
    // HUMAN COMMENT:
    // DEACTIVATE ROLE ACCESS
    // =====================================================

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
        ("COUPON", "Coupons", "/Admin/Coupons", 100),
        ("NOTIFICATION", "Notifications", "/Admin/Notifications", 110),
        ("ANALYTICS", "Analytics", "/Admin/Dashboard", 120),
        ("SUPPORT", "Support", "/Admin/UserAccess", 130),
        ("SCANNER", "Ticket Scanner", "/Admin/Security", 140),
        ("DEVELOPER", "Developer Editor", "/Developer/Index", 150)
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
}

private void EnsureDefaultRolePermissions()
{
    if (_context.RolePermissions.Any())
    {
        return;
    }

    var permissionLookup = _context.Permissions
        .AsNoTracking()
        .ToDictionary(x => x.PermissionCode, x => x.Id, StringComparer.OrdinalIgnoreCase);

    var roleLookup = _context.Roles
        .ToDictionary(x => x.RoleCode, x => x.Id, StringComparer.OrdinalIgnoreCase);

    var grants = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["AMAR_SUPER_ADMIN"] = permissionLookup.Keys.ToArray(),
        ["AMAR_ADMIN"] = permissionLookup.Keys.Where(x => !x.StartsWith("DEVELOPER_", StringComparison.OrdinalIgnoreCase)).ToArray(),
        ["AMAR_OPERATIONS_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "USER_VIEW", "SHOW_VIEW", "BOOKING_VIEW", "PAYMENT_VIEW", "REFUND_VIEW",
            "WALLET_VIEW", "COUPON_VIEW", "NOTIFICATION_VIEW", "ANALYTICS_VIEW", "SUPPORT_VIEW"
        },
        ["AMAR_BOOKING_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "BOOKING_VIEW", "BOOKING_PRINT", "BOOKING_CANCEL", "USER_VIEW", "SUPPORT_VIEW"
        },
        ["AMAR_PAYMENT_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "PAYMENT_VIEW", "PAYMENT_REFUND", "BOOKING_VIEW", "WALLET_VIEW"
        },
        ["AMAR_REFUND_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "REFUND_VIEW", "REFUND_APPROVE", "REFUND_REJECT", "REFUND_RETRY", "REFUND_UPDATE",
            "PAYMENT_VIEW", "BOOKING_VIEW"
        },
        ["AMAR_CONTENT_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "SHOW_VIEW", "SHOW_CREATE", "SHOW_UPDATE", "SHOW_DELETE",
            "COUPON_VIEW", "COUPON_CREATE", "COUPON_UPDATE", "COUPON_DELETE"
        },
        ["AMAR_NOTIFICATION_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "NOTIFICATION_VIEW", "NOTIFICATION_UPDATE", "USER_VIEW"
        },
        ["AMAR_ANALYTICS_MANAGER"] = new[]
        {
            "ADMIN_VIEW", "ANALYTICS_VIEW", "BOOKING_VIEW", "PAYMENT_VIEW", "REFUND_VIEW", "WALLET_VIEW"
        },
        ["AMAR_SUPPORT_EXECUTIVE"] = new[]
        {
            "ADMIN_VIEW", "SUPPORT_VIEW", "USER_VIEW", "BOOKING_VIEW", "PAYMENT_VIEW", "REFUND_VIEW"
        },
        ["AMAR_SCANNER_OPERATOR"] = new[]
        {
            "SCANNER_VALIDATE"
        },
        ["AMAR_USER"] = Array.Empty<string>(),
        ["ADMIN"] = permissionLookup.Keys.Where(x => !x.StartsWith("DEVELOPER_", StringComparison.OrdinalIgnoreCase)).ToArray(),
        ["USER"] = Array.Empty<string>()
    };

    long? grantedBy = null;
    if (long.TryParse(HttpContext.Session.GetString("UserId"), out var currentUserId))
    {
        grantedBy = currentUserId;
    }

    foreach (var grant in grants)
    {
        if (!roleLookup.TryGetValue(grant.Key, out var roleId))
        {
            continue;
        }

        foreach (var permissionCode in grant.Value.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!permissionLookup.TryGetValue(permissionCode, out var permissionId))
            {
                continue;
            }

            if (_context.RolePermissions.Any(x => x.RoleId == roleId && x.PermissionId == permissionId))
            {
                continue;
            }

            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow
            });
        }
    }

    _context.SaveChanges();
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
        Type = type
    };
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
        Console.WriteLine($"Notification action refund source failed: {ex.Message}");
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
        Console.WriteLine($"Notification action booking source failed: {ex.Message}");
    }

    try
    {
        var failedNotificationDeliveries = await _context.VwNotificationCenters
            .AsNoTracking()
            .Where(x =>
                x.IsError == 1 ||
                x.Status == "FAILED" ||
                x.Status == "ERROR")
            .OrderByDescending(x => x.CreatedAt)
            .Take(25)
            .ToListAsync();

        events.AddRange(failedNotificationDeliveries.Select(x => new AdminNotificationActionItem
        {
            Id = $"action-notification-{x.NotificationId}",
            Time = x.CreatedAt,
            Category = "NOTIFICATION",
            Title = string.IsNullOrWhiteSpace(x.Title) ? "Notification delivery issue" : x.Title,
            Status = string.IsNullOrWhiteSpace(x.Status) ? "FAILED" : x.Status,
            Priority = "HIGH",
            UserName = x.UserName ?? string.Empty,
            UserEmail = x.UserEmail ?? string.Empty,
            Detail = $"Type {NullText(x.NotificationType)} | Template {NullText(x.TemplateCode)} - {NullText(x.TemplateName)} | Message {NullText(x.Message)} | Retries {x.RetryCount} | Sent {FormatDateText(x.SentAt)} | Delivered {FormatDateText(x.DeliveredAt)} | Read {FormatDateText(x.ReadAt)} | Failure {NullText(x.FailureReason)}",
            ActionText = "Open Notifications",
            ActionUrl = $"/Admin/Notifications?highlight=notification-{x.NotificationId}",
            RequiresAction = true
        }));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notification action delivery source failed: {ex.Message}");
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
        Console.WriteLine($"Notification action payment source failed: {ex.Message}");
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
        Console.WriteLine($"Notification action security source failed: {ex.Message}");
    }

    try
    {
        var failedLogs = await _context.ActivityLogs
            .AsNoTracking()
            .Where(x => x.IsError > 0 || x.Status == "FAILURE")
            .OrderByDescending(x => x.CreatedAt)
            .Take(25)
            .Select(x => new
            {
                Time = x.CreatedAt,
                Type = x.Module,
                Title = x.Action,
                UserId = x.UserId,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Detail = (x.ErrorMessage ?? x.Description) ?? string.Empty,
                Status = x.Status
            })
            .ToListAsync();

        events.AddRange(failedLogs.Select(x => new AdminNotificationActionItem
        {
            Id = $"log-{x.Time.Ticks}",
            Time = x.Time,
            Category = string.IsNullOrWhiteSpace(x.Type) ? "LOG" : x.Type,
            Title = string.IsNullOrWhiteSpace(x.Title) ? "Application event" : x.Title,
            Status = string.IsNullOrWhiteSpace(x.Status) ? "FAILED" : x.Status,
            Priority = "LOW",
            UserName = x.UserId.HasValue ? $"User #{x.UserId.Value}" : string.Empty,
            Detail = $"Entity {NullText(x.EntityType)} #{(x.EntityId.HasValue ? x.EntityId.Value.ToString(CultureInfo.InvariantCulture) : "NA")} | {NullText(x.Detail)}",
            ActionText = "Open",
            ActionUrl = "/Admin/ActivityLogs",
            RequiresAction = false
        }));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Notification action log source failed: {ex.Message}");
    }

    return events
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
        Console.WriteLine($"Notification archive refund source failed: {ex.Message}");
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
        Console.WriteLine($"Notification archive delivery source failed: {ex.Message}");
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
        Console.WriteLine($"Admin notification count source failed: {ex.Message}");
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
            VenueName = "Amar Shows Multiplex",
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
