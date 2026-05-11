using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    public class VwWalletSummary
    {
        public long WalletId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public decimal BlockedBalance { get; set; }

        public int LoyaltyPoints { get; set; }

        public string WalletStatus { get; set; }

        public DateTime? LastTransactionAt { get; set; }

        public int TotalWalletTransactions { get; set; }

        


        // Human Comment:

        // PostgreSQL column:

        // wallet_balance

        [Column("wallet_balance")]

        public decimal WalletBalance { get; set; }

        // Human Comment:

        // PostgreSQL column:

        // total_credits

        [Column("total_credits")]

        public decimal TotalCredits { get; set; }

        // Human Comment:

        // PostgreSQL column:

        // total_debits

        [Column("total_debits")]

        public decimal TotalDebits { get; set; }

    }
}