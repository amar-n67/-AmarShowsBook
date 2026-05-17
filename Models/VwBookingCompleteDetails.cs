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

    [Column("total_tickets")]
    public int TotalTickets { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    [Column("payable_amount")]
    public decimal? PayableAmount { get; set; }

    [Column("transaction_ref")]
    public string? TransactionRef { get; set; } = string.Empty;

    [Column("payment_status")]
    public string? PaymentStatus { get; set; } = string.Empty;

    [Column("payment_method")]
    public string? PaymentMethod { get; set; } = string.Empty;

    [Column("gateway_name")]
    public string? GatewayName { get; set; } = string.Empty;

    [Column("transaction_status")]
    public string? TransactionStatus { get; set; } = string.Empty;

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }

    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

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
