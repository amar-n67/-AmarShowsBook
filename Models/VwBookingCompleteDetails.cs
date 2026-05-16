using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

public class VwBookingCompleteDetails
{
    [Key]
    [Column("booking_id")]
    public long BookingId { get; set; }

    // =====================================================
    // HUMAN COMMENT:
    // PostgreSQL column booked_at mapped to C# PascalCase
    // =====================================================

    [Column("booked_at")]
    public DateTime BookedAt { get; set; }

    [Column("booking_ref")]
    public string BookingRef { get; set; } = string.Empty;

    [Column("booking_status")]
    public string BookingStatus { get; set; } = string.Empty;

    [Column("seat_numbers")]
    public string? SeatNumbers { get; set; } = string.Empty;

    [Column("show_title")]
    public string ShowTitle { get; set; } = string.Empty;

    [Column("show_type")]
    public string ShowType { get; set; } = string.Empty;

    [Column("start_time")]
    public DateTime StartTime { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("payment_status")]
    public string? PaymentStatus { get; set; } = string.Empty;

    [Column("payment_method")]
    public string? PaymentMethod { get; set; } = string.Empty;

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("user_name")]
    public string? UserName { get; set; } = string.Empty;

    [Column("user_email")]
    public string? UserEmail { get; set; } = string.Empty;

    [Column("location_name")]
    public string? LocationName { get; set; } = string.Empty;

    [Column("is_error")]
    public int IsError { get; set; }
}