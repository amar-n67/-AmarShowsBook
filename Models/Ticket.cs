using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("tickets")]
public class Ticket
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("booking_id")]
    public long BookingId { get; set; }

    [Column("ticket_number")]
    public string TicketNumber { get; set; } = "";

    [Column("attendee_name")]
    public string? AttendeeName { get; set; }

    [Column("seat_number")]
    public string? SeatNumber { get; set; }

    [Column("qr_code")]
    public string? QrCode { get; set; }

    [Column("ticket_status")]
    public string? TicketStatus { get; set; }

    [Column("issued_at")]
    public DateTime? IssuedAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("qr_generated_at")]
    public DateTime? QrGeneratedAt { get; set; }

    [Column("validation_status")]
    public string? ValidationStatus { get; set; }

    [Column("validation_count")]
    public int ValidationCount { get; set; }

    [Column("last_scanned_at")]
    public DateTime? LastScannedAt { get; set; }

    [Column("last_scanned_gate")]
    public string? LastScannedGate { get; set; }

    [Column("qr_token")]
    public Guid? QrToken { get; set; }

    [Column("security_hash")]
    public string? SecurityHash { get; set; }
}
