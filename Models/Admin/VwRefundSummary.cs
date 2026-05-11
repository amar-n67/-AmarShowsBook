// Human Comment:
// Model for vw_refund_summary database view

namespace AmarShowsBook.Models.Admin
{
    public class VwRefundSummary
    {
        public long RefundId { get; set; }

        public string RefundRef { get; set; }

        public string BookingRef { get; set; }

        public string TransactionRef { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public decimal RefundAmount { get; set; }

        public string RefundReason { get; set; }

        public string RefundStatus { get; set; }

        public string RefundMethod { get; set; }

        public string GatewayRefundId { get; set; }

        public string FailureReason { get; set; }

        public DateTime RequestedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int IsRefundError { get; set; }
    }
}