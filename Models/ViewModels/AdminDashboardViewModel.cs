namespace AmarShowsBook.Models.ViewModels
{
    // Central admin analytics dashboard model
    public class AdminDashboardViewModel
    {
        // ================= BOOKINGS =================

        public int TotalBookings { get; set; }

        public int FailedBookings { get; set; }

        // ================= TRANSACTIONS =================

        public int SuccessfulPayments { get; set; }

        public int FailedPayments { get; set; }

        // ================= REFUNDS =================

        public int TotalRefunds { get; set; }

        public int FailedRefunds { get; set; }

        // ================= INVOICES =================

        public int InvoiceFailures { get; set; }

        // ================= NOTIFICATIONS =================

        public int NotificationFailures { get; set; }

        // ================= SECURITY =================

        public int TicketValidationIssues { get; set; }

        // ================= WALLET =================

        public decimal TotalWalletBalance { get; set; }

        public decimal TotalCredits { get; set; }

        public decimal TotalDebits { get; set; }
    }
}