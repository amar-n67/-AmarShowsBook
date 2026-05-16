using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("booking_drafts")]
    public class BookingDraft
    {
        [Key]
        public long Id { get; set; }

        public long UserId { get; set; }

        public int ScheduleId { get; set; }

        public string SeatNumbers { get; set; } = "";

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}