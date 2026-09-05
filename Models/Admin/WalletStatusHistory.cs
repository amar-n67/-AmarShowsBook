using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

[Table("wallet_status_history")]
public class WalletStatusHistory
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("wallet_id")]
    public long WalletId { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("previous_status")]
    public string? PreviousStatus { get; set; }

    [Column("new_status")]
    public string NewStatus { get; set; } = string.Empty;

    [Column("action_type")]
    public string ActionType { get; set; } = string.Empty;

    [Column("action_reason")]
    public string ActionReason { get; set; } = string.Empty;

    [Column("action_by")]
    public string? ActionBy { get; set; }

    [Column("wallet_balance")]
    public decimal WalletBalance { get; set; }

    [Column("blocked_balance")]
    public decimal BlockedBalance { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
