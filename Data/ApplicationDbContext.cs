using Microsoft.EntityFrameworkCore;
using AmarShowsBook.Models;

namespace AmarShowsBook.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing
        public DbSet<User> Users { get; set; }

        // 🎬 Movies
        public DbSet<Movie> Movies { get; set; }

        // 🎤 Standup
        public DbSet<StandupShow> StandupShows { get; set; }

        // 📡 Live Streams
        public DbSet<LiveStream> LiveStreams { get; set; }

        // 📍 Locations
        public DbSet<Location> Locations { get; set; }

        // 🕒 Schedule
        public DbSet<ShowSchedule> ShowSchedules { get; set; }
        // Transaction reporting SQL view
// Transaction reporting SQL view
        public DbSet<VwBookingTransactionSummary> VwBookingTransactionSummaries { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Region> Regions { get; set; }
        // Wallet analytics database view
// Wallet analytics database view
public DbSet<VwWalletSummary> VwWalletSummaries { get; set; }
        // Booking analytics view
        public DbSet<VwBookingCompleteDetails> VwBookingCompleteDetails { get; set; }

        // OPTIONAL (good practice)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
//             // Map wallet analytics SQL view
// modelBuilder.Entity<VwWalletSummary>()
//     .ToView("vw_wallet_summary")
//     .HasNoKey();
// Wallet analytics database view
modelBuilder.Entity<VwWalletSummary>(entity =>
{
    // Human comment:
    // This model comes from PostgreSQL VIEW not physical table
    entity.HasNoKey();

    // Human comment:
    // PostgreSQL view name
    entity.ToView("vw_wallet_summary");

    // Human comment:
    // C# property -> PostgreSQL column mapping

    entity.Property(e => e.WalletId)
        .HasColumnName("wallet_id");

    entity.Property(e => e.UserId)
        .HasColumnName("user_id");

    entity.Property(e => e.UserName)
        .HasColumnName("user_name");

    entity.Property(e => e.UserEmail)
        .HasColumnName("user_email");

    entity.Property(e => e.WalletBalance)
        .HasColumnName("wallet_balance");

    entity.Property(e => e.BlockedBalance)
        .HasColumnName("blocked_balance");

    entity.Property(e => e.LoyaltyPoints)
        .HasColumnName("loyalty_points");

    entity.Property(e => e.WalletStatus)
        .HasColumnName("wallet_status");

    entity.Property(e => e.LastTransactionAt)
        .HasColumnName("last_transaction_at");

    entity.Property(e => e.TotalWalletTransactions)
        .HasColumnName("total_wallet_transactions");

    entity.Property(e => e.TotalCredits)
        .HasColumnName("total_credits");

    entity.Property(e => e.TotalDebits)
        .HasColumnName("total_debits");
});
            base.OnModelCreating(modelBuilder);
            // Map transaction summary SQL view
            modelBuilder.Entity<VwBookingTransactionSummary>()
            .ToView("vw_booking_transaction_summary")
            .HasNoKey();
            // Prevent cascade issues for optional relationships
            modelBuilder.Entity<ShowSchedule>()
                .HasOne(s => s.Movie)
                .WithMany()
                .HasForeignKey(s => s.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShowSchedule>()
                .HasOne(s => s.StandupShow)
                .WithMany()
                .HasForeignKey(s => s.StandupShowId)
                .OnDelete(DeleteBehavior.Restrict);

            // // Map PostgreSQL booking details view
            // modelBuilder.Entity<VwBookingCompleteDetails>()
            //     .ToView("vw_booking_complete_details")
            //     .HasNoKey();
            // Booking complete details database view
modelBuilder.Entity<VwBookingCompleteDetails>(entity =>
{
    // Human comment:
    // This model comes from PostgreSQL VIEW, not a table.
    entity.HasNoKey();

    // Human comment:
    // PostgreSQL view name
    entity.ToView("vw_booking_complete_details");

    // Human comment:
    // C# PascalCase property -> PostgreSQL snake_case column mapping
    entity.Property(e => e.BookingId)
        .HasColumnName("booking_id");

    entity.Property(e => e.BookingRef)
        .HasColumnName("booking_ref");

    entity.Property(e => e.UserEmail)
        .HasColumnName("user_email");

    entity.Property(e => e.ShowTitle)
        .HasColumnName("show_title");

    entity.Property(e => e.BookingStatus)
        .HasColumnName("booking_status");

    entity.Property(e => e.PayableAmount)
        .HasColumnName("payable_amount");

    entity.Property(e => e.BookedAt)
        .HasColumnName("booked_at");
});

            modelBuilder.Entity<ShowSchedule>()
                .HasOne(s => s.LiveStream)
                .WithMany()
                .HasForeignKey(s => s.LiveStreamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}