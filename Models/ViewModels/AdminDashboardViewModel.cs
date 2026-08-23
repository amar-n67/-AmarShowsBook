namespace AmarShowsBook.Models.ViewModels
{
    // Central admin analytics dashboard model
    public class AdminDashboardViewModel
    {
        // ================= BOOKINGS =================

        public int TotalBookings { get; set; }

        public int FailedBookings { get; set; }

        public int TodayBookings { get; set; }

        public int ConfirmedBookings { get; set; }

        public int CancelledBookings { get; set; }

        public int TotalTickets { get; set; }

        public decimal GrossBookingAmount { get; set; }

        public decimal PayableBookingAmount { get; set; }

        // ================= TRANSACTIONS =================

        public int SuccessfulPayments { get; set; }

        public int FailedPayments { get; set; }

        public decimal SuccessfulPaymentAmount { get; set; }

        // ================= REFUNDS =================

        public int TotalRefunds { get; set; }

        public int FailedRefunds { get; set; }

        public int PendingRefunds { get; set; }

        public int ApprovedRefunds { get; set; }

        public int RejectedRefunds { get; set; }

        public decimal RequestedRefundAmount { get; set; }

        // ================= INVOICES =================

        public int InvoiceFailures { get; set; }

        // ================= NOTIFICATIONS =================

        public int NotificationFailures { get; set; }

        public int TotalNotifications { get; set; }

        public int DeliveredNotifications { get; set; }

        public int PendingNotifications { get; set; }

        public int HighPriorityNotifications { get; set; }

        // ================= SECURITY =================

        public int TicketValidationIssues { get; set; }

        public int ValidatedTickets { get; set; }

        // ================= WALLET =================

        public decimal TotalWalletBalance { get; set; }

        public decimal TotalCredits { get; set; }

        public decimal TotalDebits { get; set; }

        public decimal BlockedWalletBalance { get; set; }

        // ================= CONTENT / ACCESS =================

        public int TotalUsers { get; set; }

        public int TotalMovies { get; set; }

        public int TotalStandups { get; set; }

        public int TotalLiveStreams { get; set; }

        public int TotalSchedules { get; set; }

        public int UpcomingSchedules { get; set; }

        public int TodaySchedules { get; set; }

        public int TotalScreens { get; set; }

        public int TotalVenues { get; set; }

        public int ActiveRoles { get; set; }

        public List<DashboardBreakdownItem> BookingStatusBreakdown { get; set; } = new();

        public List<DashboardBreakdownItem> ShowTypeBreakdown { get; set; } = new();

        public List<DashboardBreakdownItem> PaymentMethodBreakdown { get; set; } = new();

        public List<DashboardRecentItem> RecentBookings { get; set; } = new();

        public List<DashboardRecentItem> RecentRefunds { get; set; } = new();
    }

    public class DashboardBreakdownItem
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Amount { get; set; }
    }

    public class DashboardRecentItem
    {
        public string Title { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? Time { get; set; }
    }
}
