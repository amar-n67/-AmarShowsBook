namespace AmarShowsBook.Models
{
    public class PaymentSession
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string SessionToken { get; set; }

        public string Status { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}