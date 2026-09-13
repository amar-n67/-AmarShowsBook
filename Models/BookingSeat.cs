using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("booking_seats")]
public class BookingSeat
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("booking_id")]
    public long BookingId { get; set; }

    [Column("screen_seat_id")]
    public long ScreenSeatId { get; set; }

    [Column("booking_item_id")]
    public long? BookingItemId { get; set; }

    [Column("seat_price")]
    public decimal? SeatPrice { get; set; }

    [Column("booking_status")]
    public string? BookingStatus { get; set; }

    [Column("qr_code")]
    public string? QrCode { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
