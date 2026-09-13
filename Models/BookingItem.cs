using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("booking_items")]
public class BookingItem
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("booking_id")]
    public long BookingId { get; set; }

    [Column("ticket_type")]
    public string? TicketType { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("attendee_name")]
    public string? AttendeeName { get; set; }

    [Column("attendee_mobile")]
    public string? AttendeeMobile { get; set; }

    [Column("attendee_email")]
    public string? AttendeeEmail { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
