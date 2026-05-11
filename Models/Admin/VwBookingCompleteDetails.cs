namespace AmarShowsBook.Models.Admin
{
    public class VwBookingCompleteDetails
    {
        public long BookingId { get; set; }

        public string BookingRef { get; set; }

        public string UserName { get; set; }

        public string ShowTitle { get; set; }

        public decimal PayableAmount { get; set; }

        public string BookingStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}