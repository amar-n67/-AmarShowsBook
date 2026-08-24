using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

[Table("vw_wallet_summary")]
public class VwWalletSummary
{
    [Key]
    [Column("wallet_id")]
    public long WalletId { get; set; }

    // =====================================================
    // HUMAN COMMENT:
    // USER BASIC DETAILS
    // =====================================================

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("user_name")]
    public string? UserName { get; set; }

    [Column("user_email")]
    public string? UserEmail { get; set; }

    // =====================================================
    // HUMAN COMMENT:
    // WALLET MONEY DETAILS
    // =====================================================

    [Column("wallet_balance")]
    public decimal WalletBalance { get; set; }

    [Column("blocked_balance")]
    public decimal BlockedBalance { get; set; }

    [Column("total_credits")]
    public decimal TotalCredits { get; set; }

    [Column("total_debits")]
    public decimal TotalDebits { get; set; }

    // =====================================================
    // HUMAN COMMENT:
    // WALLET STATUS
    // =====================================================

    [Column("wallet_status")]
    public string? WalletStatus { get; set; }

    [Column("suspension_reason")]
    public string? SuspensionReason { get; set; }

    [Column("suspended_at")]
    public DateTime? SuspendedAt { get; set; }

    [Column("suspended_by")]
    public string? SuspendedBy { get; set; }

    [Column("reactivated_at")]
    public DateTime? ReactivatedAt { get; set; }

    [Column("reactivated_by")]
    public string? ReactivatedBy { get; set; }

    [Column("reactivation_reason")]
    public string? ReactivationReason { get; set; }

    [Column("loyalty_points")]
    public long LoyaltyPoints { get; set; }

    [Column("total_wallet_transactions")]
    public long TotalWalletTransactions { get; set; }

    [Column("last_transaction_at")]
    public DateTime? LastTransactionAt { get; set; }
}
