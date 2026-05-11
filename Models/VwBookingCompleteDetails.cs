using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    [Keyless]
    public class VwBookingCompleteDetails
    {
        public int BookingId { get; set; }

        public string BookingRef { get; set; }

        public string UserEmail { get; set; }

        public string ShowTitle { get; set; }

        public string BookingStatus { get; set; }

        public decimal PayableAmount { get; set; }

        public DateTime BookedAt { get; set; }
    }
}