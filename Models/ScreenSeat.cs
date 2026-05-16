using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("screen_seats")]
    public class ScreenSeat
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("screen_id")]
        public long ScreenId { get; set; }

        [Column("seat_row")]
        public string SeatRow { get; set; }

        [Column("seat_number")]
        public string SeatNumber { get; set; }

        [Column("seat_category")]
        public string SeatCategory { get; set; }

        [Column("seat_price")]
        public decimal SeatPrice { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
         public int ScheduleId { get; set; }
[Column("schedule_id")]
    public ShowSchedule? Schedule { get; set; }
    }
}