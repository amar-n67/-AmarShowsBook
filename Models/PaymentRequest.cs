namespace AmarShowsBook.Models
{
    public class PaymentRequest
    {
        public long BookingId { get; set; }

        public string PaymentMethod { get; set; }="";

        public string? UpiName { get; set; }

        public string? UpiId { get; set; }

        public string? CardNumber { get; set; }

        public string? CardExpiry { get; set; }

        public string? NetBank { get; set; }

        public string? NetBankUserId { get; set; }

        public bool OtpVerified { get; set; }
    }
}
