namespace AmarShowsBook.Models.Admin
{
    public class VwBookingTransactionSummary
    {
        public long TransactionId { get; set; }

        public string TransactionRef { get; set; }

        public string UserName { get; set; }

        public decimal TransactionAmount { get; set; }

        public string PaymentMethod { get; set; }

        public string TransactionStatus { get; set; }

        public DateTime TransactionCreatedAt { get; set; }
    }
}