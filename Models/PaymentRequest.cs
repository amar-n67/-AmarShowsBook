namespace AmarShowsBook.Models
{
    public class PaymentRequest
    {
        public long BookingId { get; set; }

        public string PaymentMethod { get; set; }="";
    }
}