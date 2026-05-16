using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("transactions")]
public class Transaction
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("transaction_ref")]
    public string? TransactionRef { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("transaction_type")]
    public string? TransactionType { get; set; }

    [Column("payment_method")]
    public string? PaymentMethod { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string? Currency { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("gateway_name")]
    public string? GatewayName { get; set; }

    [Column("gateway_transaction_id")]
    public string? GatewayTransactionId { get; set; }

    [Column("booking_id")]
    public long? BookingId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("failure_reason")]
    public string? FailureReason { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("initiated_at")]
    public DateTime? InitiatedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("gateway_status_code")]
    public string? GatewayStatusCode { get; set; }

    [Column("refunded_amount")]
    public decimal? RefundedAmount { get; set; }

    [Column("refund_status")]
    public string? RefundStatus { get; set; }

    [Column("reconciliation_status")]
    public string? ReconciliationStatus { get; set; }

    [Column("fraud_score")]
    public decimal? FraudScore { get; set; }

    [Column("is_suspicious")]
    public bool? IsSuspicious { get; set; }

    [Column("device_fingerprint")]
    public string? DeviceFingerprint { get; set; }

    [Column("payment_source")]
    public string? PaymentSource { get; set; }

    [Column("retry_count")]
    public int? RetryCount { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }
}