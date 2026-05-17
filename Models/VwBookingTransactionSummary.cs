using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    // Map SQL view: vw_booking_transaction_summary
    [Keyless]
    public class VwBookingTransactionSummary
    {
        [Column("booking_id")]
        public long BookingId { get; set; }

        [Column("booking_ref")]
        public string BookingRef { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }

        [Column("user_email")]
        public string UserEmail { get; set; }

        [Column("show_type")]
        public string ShowType { get; set; }

        [Column("show_title")]
        public string ShowTitle { get; set; }

        [Column("booking_status")]
        public string BookingStatus { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("transaction_id")]
        public long? TransactionId { get; set; }

        [Column("transaction_ref")]
        public string TransactionRef { get; set; }

        [Column("payment_method")]
        public string PaymentMethod { get; set; }

        [Column("transaction_amount")]
        public decimal? TransactionAmount { get; set; }

        [Column("currency")]
        public string Currency { get; set; }

        [Column("transaction_status")]
        public string TransactionStatus { get; set; }

        [Column("gateway_name")]
        public string GatewayName { get; set; }

        [Column("failure_reason")]
        public string FailureReason { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("booking_created_at")]
        public DateTime BookingCreatedAt { get; set; }

        [Column("is_payment_error")]
        public int IsPaymentError { get; set; }
    }
}
