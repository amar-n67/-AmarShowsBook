using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    // PostgreSQL wallet summary view
    [Keyless]
    public class VwWalletSummary
    {
        [Column("wallet_id")]
        public long WalletId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_name")]
        public string? UserName { get; set; }

        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("wallet_balance")]
        public decimal WalletBalance { get; set; }

        // PostgreSQL snake_case column
        [Column("blocked_balance")]
        public decimal BlockedBalance { get; set; }

        [Column("loyalty_points")]
        public int LoyaltyPoints { get; set; }

        [Column("wallet_status")]
        public string? WalletStatus { get; set; }

        [Column("last_transaction_at")]
        public DateTime? LastTransactionAt { get; set; }

        [Column("total_wallet_transactions")]
        public long TotalWalletTransactions { get; set; }

        [Column("total_credits")]
        public decimal TotalCredits { get; set; }

        [Column("total_debits")]
        public decimal TotalDebits { get; set; }
    }
}