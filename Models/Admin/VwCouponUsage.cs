using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

public class VwCouponUsage
{
    [Column("usage_id")]
    public long UsageId { get; set; }

    [Column("coupon_id")]
    public long CouponId { get; set; }

    [Column("coupon_code")]
    public string? CouponCode { get; set; }

    [Column("booking_id")]
    public long? BookingId { get; set; }

    [Column("booking_ref")]
    public string? BookingRef { get; set; }

    [Column("transaction_id")]
    public long? TransactionId { get; set; }

    [Column("transaction_ref")]
    public string? TransactionRef { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("user_name")]
    public string? UserName { get; set; }

    [Column("user_email")]
    public string? UserEmail { get; set; }

    [Column("show_name")]
    public string? ShowName { get; set; }

    [Column("show_type")]
    public string? ShowType { get; set; }

    [Column("show_time")]
    public DateTime? ShowTime { get; set; }

    [Column("original_amount")]
    public decimal? OriginalAmount { get; set; }

    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    [Column("final_amount")]
    public decimal? FinalAmount { get; set; }

    [Column("usage_status")]
    public string? UsageStatus { get; set; }

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }
}
