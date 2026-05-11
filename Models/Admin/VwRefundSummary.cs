using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    [Table("vw_refund_summary")]
    public class VwRefundSummary
    {
        [Column("refund_id")]
        public long RefundId { get; set; }

        [Column("refund_ref")]
        public string? RefundRef { get; set; }

        [Column("booking_ref")]
        public string? BookingRef { get; set; }

        [Column("transaction_ref")]
        public string? TransactionRef { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_name")]
        public string? UserName { get; set; }

        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("refund_amount")]
        public decimal RefundAmount { get; set; }

        [Column("refund_reason")]
        public string? RefundReason { get; set; }

        [Column("refund_status")]
        public string? RefundStatus { get; set; }

        [Column("refund_method")]
        public string? RefundMethod { get; set; }

        [Column("gateway_refund_id")]
        public string? GatewayRefundId { get; set; }

        [Column("failure_reason")]
        public string? FailureReason { get; set; }

        [Column("requested_at")]
        public DateTime RequestedAt { get; set; }

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Human comment:
        // PostgreSQL view column is "is_refund_error"
        // but C# uses PascalCase naming convention.
        [Column("is_refund_error")]
        public int IsRefundError { get; set; }
    }
}