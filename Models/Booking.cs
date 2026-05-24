using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("bookings")]
public class Booking
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("booking_ref")]
    public string BookingRef { get; set; } = "";

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    [Column("booking_status")]
    public string BookingStatus { get; set; } = "PENDING";

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("total_tickets")]
    public int TotalTickets { get; set; }

    [Column("booking_source")]
    public string? BookingSource { get; set; }

    [Column("booked_at")]
    public DateTime? BookedAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [Column("original_amount")]
    public decimal? OriginalAmount { get; set; }

    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    [Column("coupon_id")]
    public long? CouponId { get; set; }

    [Column("payable_amount")]
    public decimal? PayableAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("convenience_fee")]
    public decimal? ConvenienceFee { get; set; }

    [Column("wallet_amount_used")]
    public decimal? WalletAmountUsed { get; set; }

    [Column("transaction_id")]
    public long? TransactionId { get; set; }

    [Column("payment_status")]
    public string? PaymentStatus { get; set; }

    [Column("confirmed_at")]
    public DateTime? ConfirmedAt { get; set; }
}
