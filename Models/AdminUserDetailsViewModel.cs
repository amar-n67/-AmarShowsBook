namespace AmarShowsBook.Models.ViewModels
{
    public class AdminUserDetailsViewModel
    {

        public long UserId { get; set; }
        public List<string> UserAccess { get; set; } = new();

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? Language { get; set; }

        public string? Genre { get; set; }

        public string? Country { get; set; }

        public string? State { get; set; }

        public string? District { get; set; }

        public string? Address { get; set; }

        public string? Pincode { get; set; }

        public string? ProfileImagePath { get; set; }


        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? RegisteredAt { get; set; }

        public DateTime? LastLoginAt { get; set; }


        public decimal WalletBalance { get; set; }
        public decimal BlockedBalance { get; set; }
        public decimal WalletCredits { get; set; }
        public decimal WalletDebits { get; set; }
        public long LoyaltyPoints { get; set; }
        public string? WalletStatus { get; set; }
        public long TotalWalletTransactions { get; set; }
        public DateTime? LastWalletTransactionAt { get; set; }

        public int TotalTransactions { get; set; }

        public int SuccessTransactions { get; set; }

        public int FailedTransactions { get; set; }

        public int PendingTransactions { get; set; }

        public decimal TotalSpent { get; set; }


        public string? LastTransactionRef { get; set; }

        public string? LastTransactionStatus { get; set; }

        public DateTime? LastTransactionDate { get; set; }
public List<VwBookingTransactionSummary>

            LastTransactions { get; set; }

                = new();

        public List<VwBookingCompleteDetails> Bookings { get; set; }
            = new();

        public List<string>

            RecentActivities { get; set; }

                = new();
    }
}
