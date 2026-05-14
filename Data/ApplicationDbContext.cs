using Microsoft.EntityFrameworkCore;

using AmarShowsBook.Models;
using AmarShowsBook.Models.Admin;
using AmarShowsBook.Models.ViewModels;

namespace AmarShowsBook.Data
{
    public class ApplicationDbContext : DbContext
    {
        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================================
        // MAIN TABLES
        // =====================================================

        public DbSet<User> Users { get; set; }

        public DbSet<DeletedUser> DeletedUsers { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }

        public DbSet<Movie> Movies { get; set; }

        public DbSet<StandupShow> StandupShows { get; set; }

        public DbSet<LiveStream> LiveStreams { get; set; }

        public DbSet<Location> Locations { get; set; }

        public DbSet<ShowSchedule> ShowSchedules { get; set; }

        public DbSet<Country> Countries { get; set; }

        public DbSet<State> States { get; set; }

        public DbSet<District> Districts { get; set; }

        public DbSet<Region> Regions { get; set; }

        // =====================================================
        // USER ROLE ACCESS TABLES
        // =====================================================

        public DbSet<UserRoleMapping> UserRoleMappings { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        // =====================================================
        // ADMIN SQL VIEW TABLES
        // =====================================================

        public DbSet<VwEnterpriseActivityLog>
            VwEnterpriseActivityLogs { get; set; }

        public DbSet<VwBookingCompleteDetails>
            VwBookingCompleteDetails { get; set; }

        public DbSet<VwBookingTransactionSummary>
            VwBookingTransactionSummaries { get; set; }

        public DbSet<VwRefundSummary>
            VwRefundSummaries { get; set; }

        public DbSet<VwWalletSummary>
            VwWalletSummaries { get; set; }

        public DbSet<VwNotificationCenter>
            VwNotificationCenters { get; set; }

        public DbSet<VwInvoiceSummary>
            VwInvoiceSummaries { get; set; }

        public DbSet<VwTicketValidationSummary>
            VwTicketValidationSummaries { get; set; }

        public DbSet<VwUserAccessMatrix>
            VwUserAccessMatrices { get; set; }

        public DbSet<VwUserApplicationMenu>
            VwUserApplicationMenus { get; set; }

        public DbSet<VwAdminUserManagement>
            VwAdminUserManagement { get; set; }

        // =====================================================
        // ADMIN TRANSACTION VIEW
        // =====================================================

        public DbSet<AdminTransactionViewModel>
            AdminTransactions { get; set; }

        // =====================================================
        // MODEL CONFIGURATION
        // =====================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // ACTIVITY LOG TABLE
            // =====================================================

            modelBuilder.Entity<ActivityLog>()
                .ToTable("activity_logs");

            // =====================================================
            // ENTERPRISE ACTIVITY LOG VIEW
            // =====================================================

            modelBuilder.Entity<VwEnterpriseActivityLog>()
                .HasNoKey()
                .ToView("vw_enterprise_activity_logs");

            // =====================================================
            // ADMIN TRANSACTION VIEW
            // =====================================================

            modelBuilder.Entity<AdminTransactionViewModel>()
                .HasNoKey()
                .ToView("vw_admin_transaction_complete");

            // =====================================================
            // SHOW SCHEDULE RELATIONSHIPS
            // =====================================================

            modelBuilder.Entity<ShowSchedule>()
                .HasOne(x => x.Movie)
                .WithMany()
                .HasForeignKey(x => x.MovieId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShowSchedule>()
                .HasOne(x => x.StandupShow)
                .WithMany()
                .HasForeignKey(x => x.StandupShowId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShowSchedule>()
                .HasOne(x => x.LiveStream)
                .WithMany()
                .HasForeignKey(x => x.LiveStreamId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // ADMIN SQL VIEW MAPPINGS
            // =====================================================

            modelBuilder.Entity<VwBookingCompleteDetails>()
                .HasNoKey()
                .ToView("vw_booking_complete_details");

            modelBuilder.Entity<VwBookingTransactionSummary>()
                .HasNoKey()
                .ToView("vw_booking_transaction_summary");

            modelBuilder.Entity<VwRefundSummary>()
                .HasNoKey()
                .ToView("vw_refund_summary");

            modelBuilder.Entity<VwWalletSummary>()
                .HasNoKey()
                .ToView("vw_wallet_summary");

            modelBuilder.Entity<VwNotificationCenter>()
                .HasNoKey()
                .ToView("vw_notification_center");

            modelBuilder.Entity<VwInvoiceSummary>()
                .HasNoKey()
                .ToView("vw_invoice_summary");

            modelBuilder.Entity<VwTicketValidationSummary>()
                .HasNoKey()
                .ToView("vw_ticket_validation_summary");

            modelBuilder.Entity<VwUserAccessMatrix>()
                .HasNoKey()
                .ToView("vw_user_access_matrix");

            modelBuilder.Entity<VwUserApplicationMenu>()
                .HasNoKey()
                .ToView("vw_user_application_menus");

            modelBuilder.Entity<VwAdminUserManagement>()
                .HasNoKey()
                .ToView("vw_admin_user_management");
        }
    }
}