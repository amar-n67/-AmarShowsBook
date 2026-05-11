using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Map SQL view: vw_booking_transaction_summary
    [Keyless]
    public class VwBookingTransactionSummary
    {
        public int BookingId { get; set; }

        public string BookingRef { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string ShowType { get; set; }

        public string ShowTitle { get; set; }

        public string BookingStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public int? TransactionId { get; set; }

        public string TransactionRef { get; set; }

        public string PaymentMethod { get; set; }

        public decimal? TransactionAmount { get; set; }

        public string Currency { get; set; }

        public string TransactionStatus { get; set; }

        public string GatewayName { get; set; }

        public string FailureReason { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime BookingCreatedAt { get; set; }

        public int IsPaymentError { get; set; }
    }
}