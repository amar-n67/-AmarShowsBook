using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Booking analytics SQL view
    [Keyless]
    public class VwBookingCompleteDetails
    {
        public int BookingId { get; set; }

        public string? BookingRef { get; set; }

        public string? UserEmail { get; set; }

        public string? ShowTitle { get; set; }

        public string? BookingStatus { get; set; }

        public string? PaymentStatus { get; set; }

        public decimal PayableAmount { get; set; }

        // Maps PostgreSQL computed column:
        // CASE WHEN booking failed THEN 1 ELSE 0
        public int IsError { get; set; }
    }
}