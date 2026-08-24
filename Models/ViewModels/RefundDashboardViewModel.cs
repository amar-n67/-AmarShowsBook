using AmarShowsBook.Models.Admin;

namespace AmarShowsBook.Models.ViewModels
{
    public class RefundDashboardViewModel
    {

        public List<VwRefundSummary> Refunds { get; set; }
            = new();


        public int TotalRefunds { get; set; }

        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        public int PendingCount { get; set; }

        public int RejectedCount { get; set; }


        public decimal TotalRefundAmount { get; set; }


        public double SuccessRate { get; set; }
    }
}