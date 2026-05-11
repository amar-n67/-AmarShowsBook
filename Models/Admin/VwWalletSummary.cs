// Human Comment:
// Model for vw_wallet_summary database view

namespace AmarShowsBook.Models.Admin
{
    public class VwWalletSummary
    {
        public long WalletId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public decimal WalletBalance { get; set; }

        public decimal BlockedBalance { get; set; }

        public int LoyaltyPoints { get; set; }

        public string WalletStatus { get; set; }

        public DateTime? LastTransactionAt { get; set; }

        public int TotalWalletTransactions { get; set; }

        public decimal TotalCredits { get; set; }

        public decimal TotalDebits { get; set; }
    }
}