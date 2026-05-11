using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    // SQL analytics view for booking dashboard
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

        // Human readable booking timestamp
        // Used in booking history + admin analytics
        public DateTime? BookedAt { get; set; }

        // Computed SQL error flag
        // 1 = failed booking
        // 0 = successful booking
       // public int IsError { get; set; }
        // Maps SQL snake_case column to C# PascalCase property
[Column("is_error")]
public int IsError { get; set; }
    }
}