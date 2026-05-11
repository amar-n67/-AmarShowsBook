namespace AmarShowsBook.Models.Admin
{
    public class VwWalletSummary
    {
        public long WalletId { get; set; }

        public string UserName { get; set; }

        public decimal WalletBalance { get; set; }

        public decimal BlockedBalance { get; set; }

        public int LoyaltyPoints { get; set; }
    }
}