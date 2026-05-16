using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("seat_locks")]
    public class SeatLock
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long? UserId { get; set; }

        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("screen_seat_id")]
        public long ScreenSeatId { get; set; }

        [Column("locked_at")]
        public DateTime LockedAt { get; set; }

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [Column("lock_status")]
        public string LockStatus { get; set; }
    }
}