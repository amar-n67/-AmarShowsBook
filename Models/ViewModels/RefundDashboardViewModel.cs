using AmarShowsBook.Models.Admin;

namespace AmarShowsBook.Models.ViewModels
{
    public class RefundDashboardViewModel
    {
        // =====================================================
        // REFUND LIST
        // =====================================================

        public List<VwRefundSummary> Refunds { get; set; }
            = new();

        // =====================================================
        // DASHBOARD COUNTS
        // =====================================================

        public int TotalRefunds { get; set; }

        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        public int PendingCount { get; set; }

        public int RejectedCount { get; set; }

        // =====================================================
        // REFUND TOTALS
        // =====================================================

        public decimal TotalRefundAmount { get; set; }

        // =====================================================
        // ANALYTICS
        // =====================================================

        public double SuccessRate { get; set; }
    }
}