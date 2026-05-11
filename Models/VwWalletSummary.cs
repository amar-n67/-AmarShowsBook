using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Represents PostgreSQL wallet analytics view
    [Keyless]
    public class VwWalletSummary
    {
        public int WalletId { get; set; }

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