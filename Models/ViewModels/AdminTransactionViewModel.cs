namespace AmarShowsBook.Models.ViewModels
{
    public class AdminTransactionViewModel
    {
        public long TransactionId { get; set; }

        public string? TransactionRef { get; set; }

        public string? TransactionType { get; set; }

        public string? PaymentMethod { get; set; }

        public string? GatewayName { get; set; }

        public decimal Amount { get; set; }

        public string? Currency { get; set; }

        public string? TransactionStatus { get; set; }

        public string? UserName { get; set; }

        public string? UserEmail { get; set; }

        public string? BookingRef { get; set; }

        public string? BookingStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal PayableAmount { get; set; }

        public string? CouponCode { get; set; }

        public string? ShowType { get; set; }

        public string? ShowTitle { get; set; }

        public string? Country { get; set; }

        public string? State { get; set; }

        public string? Area { get; set; }

        public string? RefundStatus { get; set; }

        public decimal? RefundAmount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}