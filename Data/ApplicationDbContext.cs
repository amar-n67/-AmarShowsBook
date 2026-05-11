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
public DbSet<VwWalletSummary> VwWalletSummary { get; set; }
        // Booking analytics view
        public DbSet<VwBookingCompleteDetails> VwBookingCompleteDetails { get; set; }

        // OPTIONAL (good practice)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            // Map PostgreSQL booking details view
            modelBuilder.Entity<VwBookingCompleteDetails>()
                .ToView("vw_booking_complete_details")
                .HasNoKey();

            modelBuilder.Entity<ShowSchedule>()
                .HasOne(s => s.LiveStream)
                .WithMany()
                .HasForeignKey(s => s.LiveStreamId)
                .OnDelete(DeleteBehavior.Restrict);
            // Map wallet summary PostgreSQL view
            modelBuilder.Entity<VwWalletSummary>()
                .ToView("vw_wallet_summary")
                .HasNoKey();
        }
    }
}