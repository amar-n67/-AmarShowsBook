namespace AmarShowsBook.Models.ViewModels
{
    // =========================================================
    // HUMAN COMMENT:
    // Enterprise admin transaction monitoring model
    // Added nullable properties to safely handle NULL DB values
    // =========================================================
    public class AdminTransactionViewModel
    {
        public long TransactionId { get; set; }

        public string? TransactionRef { get; set; }

        public string? BookingRef { get; set; }

        public string? UserName { get; set; }

        public string? UserEmail { get; set; }

        public string? ShowTitle { get; set; }

        public string? BookingStatus { get; set; }

        public string? RefundStatus { get; set; }

        public decimal Amount { get; set; }

        public decimal? DiscountAmount { get; set; }

        public string? CouponCode { get; set; }

        public string? PaymentMethod { get; set; }

        public string? GatewayName { get; set; }

        public string? Currency { get; set; }

        public string? TransactionStatus { get; set; }

        public string? GatewayTransactionId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}