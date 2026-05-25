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
    public DbSet<HomeShowViewModel> HomeShows { get; set; }
        public DbSet<BookingDraft> BookingDrafts { get; set; }
        public DbSet<PaymentSession> PaymentSessions { get; set; }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BookingItem> BookingItems { get; set; }
        public DbSet<BookingSeat> BookingSeats { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

public DbSet<BookingTransaction> BookingTransactions { get; set; }

public DbSet<SeatLock> SeatLocks { get; set; }

public DbSet<DummyCard> DummyCards { get; set; }
        public DbSet<RefundActionLog> RefundActionLogs { get; set; }

        public DbSet<Refund> Refunds { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<ScreenSeat> ScreenSeats { get; set; }
        public DbSet<Screen> Screens { get; set; }
        public DbSet<Venue> Venues { get; set; }
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
        public DbSet<ApplicationModule> ApplicationModules { get; set; }
        public DbSet<ApplicationMenu> ApplicationMenus { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

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
    // =========================================
    // TABLES
    // =========================================

    modelBuilder.Entity<BookingDraft>()
        .ToTable("booking_drafts");

    modelBuilder.Entity<BookingTransaction>()
        .ToTable("booking_transactions");

    modelBuilder.Entity<DummyCard>()
        .ToTable("dummy_cards");

    modelBuilder.Entity<Refund>()
        .ToTable("refunds");

    modelBuilder.Entity<ActivityLog>()
        .ToTable("activity_logs");

    modelBuilder.Entity<SeatLock>()
        .ToTable("seat_locks");

    modelBuilder.Entity<Booking>(entity =>
    {
        entity.ToTable("bookings");

        entity.Property(x=>x.BookedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.ConfirmedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.CreatedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.WalletAmountUsed)
            .HasColumnName("wallet_amount_used");
    });

    modelBuilder.Entity<Venue>(entity =>
    {
        entity.ToTable("venues");
        entity.HasKey(x=>x.Id);
    });

    modelBuilder.Entity<Transaction>(entity =>
    {
        entity.ToTable("transactions");

        entity.Property(x=>x.InitiatedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.CompletedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.CreatedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.UpdatedAt)
            .HasColumnType("timestamp without time zone");
    });

    modelBuilder.Entity<BookingItem>(entity =>
    {
        entity.ToTable("booking_items");

        entity.Property(x=>x.CreatedAt)
            .HasColumnType("timestamp without time zone");
    });

    modelBuilder.Entity<BookingSeat>(entity =>
    {
        entity.ToTable("booking_seats");

        entity.Property(x=>x.CreatedAt)
            .HasColumnType("timestamp without time zone");
    });

    modelBuilder.Entity<Ticket>(entity =>
    {
        entity.ToTable("tickets");

        entity.Property(x=>x.IssuedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.CreatedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        entity.Property(x=>x.QrGeneratedAt)
            .HasColumnType("timestamp without time zone");
    });



    // =========================================
    // SCREEN
    // =========================================

    modelBuilder.Entity<Screen>(entity =>
    {
        entity.ToTable("screens");

        entity.HasKey(x=>x.Id);

        entity.Property(x=>x.Id)
            .HasColumnName("id");

        entity.Property(x=>x.VenueId)
            .HasColumnName("venue_id");

        entity.Property(x=>x.ScreenCode)
            .HasColumnName("screen_code");

        entity.Property(x=>x.ScreenName)
            .HasColumnName("screen_name");

        entity.Property(x=>x.TotalSeats)
            .HasColumnName("total_seats");

        entity.Property(x=>x.ScreenType)
            .HasColumnName("screen_type");

        entity.Property(x=>x.AudioSystem)
            .HasColumnName("audio_system");

        entity.Property(x=>x.IsActive)
            .HasColumnName("is_active");

        entity.Property(x=>x.CreatedAt)
            .HasColumnName("created_at");

        entity.Property(x=>x.UpdatedAt)
            .HasColumnName("updated_at");
    });

    modelBuilder.Entity<ApplicationModule>(entity =>
    {
        entity.ToTable("application_modules");
        entity.HasKey(x=>x.Id);
    });

    modelBuilder.Entity<ApplicationMenu>(entity =>
    {
        entity.ToTable("application_menus");
        entity.HasKey(x=>x.Id);
    });

    modelBuilder.Entity<RolePermission>(entity =>
    {
        entity.ToTable("role_permissions");
        entity.HasKey(x=>x.Id);
    });



    // =========================================
    // SCREEN SEATS
    // =========================================

    modelBuilder.Entity<ScreenSeat>(entity =>
    {
        entity.ToTable("screen_seats");

        entity.HasKey(x=>x.Id);

        entity.Property(x=>x.Id)
            .HasColumnName("id");

        entity.Property(x=>x.ScreenId)
            .HasColumnName("screen_id");

        entity.Property(x=>x.ScheduleId)
            .HasColumnName("ScheduleId");

        entity.Property(x=>x.SeatRow)
            .HasColumnName("seat_row");

        entity.Property(x=>x.SeatNumber)
            .HasColumnName("seat_number");

        entity.Property(x=>x.SeatCategory)
            .HasColumnName("seat_category");

        entity.Property(x=>x.SeatPrice)
            .HasColumnName("seat_price");

        entity.Property(x=>x.IsActive)
            .HasColumnName("is_active");

        entity.HasOne(x=>x.Screen)
            .WithMany()
            .HasForeignKey(x=>x.ScreenId);

            
    });



    // =========================================
    // SHOW RELATIONSHIPS
    // =========================================

    modelBuilder.Entity<ShowSchedule>()
        .ToTable("ShowSchedules");

    modelBuilder.Entity<ShowSchedule>()
        .Property(x=>x.ScreenId)
        .HasColumnName("screen_id");

    modelBuilder.Entity<ShowSchedule>()
        .HasOne(x=>x.Movie)
        .WithMany()
        .HasForeignKey(x=>x.MovieId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ShowSchedule>()
        .HasOne(x=>x.StandupShow)
        .WithMany()
        .HasForeignKey(x=>x.StandupShowId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ShowSchedule>()
        .HasOne(x=>x.LiveStream)
        .WithMany()
        .HasForeignKey(x=>x.LiveStreamId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ShowSchedule>()
        .HasOne(x=>x.Location)
        .WithMany()
        .HasForeignKey(x=>x.LocationId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ShowSchedule>()
        .HasOne(x=>x.Screen)
        .WithMany()
        .HasForeignKey(x=>x.ScreenId)
        .OnDelete(DeleteBehavior.Restrict);



    // =========================================
    // VIEWS
    // =========================================

    modelBuilder.Entity<HomeShowViewModel>(entity =>
    {
        entity.HasNoKey();

        entity.ToView(
        "vw_home_show_listing");

        entity.Property(x=>x.ScheduleId)
            .HasColumnName("schedule_id");

        entity.Property(x=>x.ShowType)
            .HasColumnName("show_type");

        entity.Property(x=>x.ShowId)
            .HasColumnName("show_id");

        entity.Property(x=>x.Title)
            .HasColumnName("title");

        entity.Property(x=>x.StartTime)
            .HasColumnName("start_time");

        entity.Property(x=>x.EndTime)
            .HasColumnName("end_time");

        entity.Property(x=>x.Location)
            .HasColumnName("location");

        entity.Property(x=>x.State)
            .HasColumnName("state");

        entity.Property(x=>x.Country)
            .HasColumnName("country");

        entity.Ignore(x=>x.TheaterDetails);
    });


    modelBuilder.Entity<VwEnterpriseActivityLog>()
        .HasNoKey()
        .ToView(
        "vw_enterprise_activity_logs");

    modelBuilder.Entity<AdminTransactionViewModel>()
        .HasNoKey()
        .ToView(
        "vw_admin_transaction_complete");

    modelBuilder.Entity<VwBookingCompleteDetails>()
        .HasNoKey()
        .ToView(
        "vw_booking_complete_details");

    modelBuilder.Entity<VwBookingTransactionSummary>()
        .HasNoKey()
        .ToView(
        "vw_booking_transaction_summary");

    modelBuilder.Entity<VwRefundSummary>()
        .HasNoKey()
        .ToView(
        "vw_refund_summary");

    modelBuilder.Entity<VwWalletSummary>()
        .HasNoKey()
        .ToView(
        "vw_wallet_summary");

    modelBuilder.Entity<VwNotificationCenter>()
        .HasNoKey()
        .ToView(
        "vw_notification_center");

    modelBuilder.Entity<VwInvoiceSummary>()
        .HasNoKey()
        .ToView(
        "vw_invoice_summary");

    modelBuilder.Entity<VwTicketValidationSummary>()
        .HasNoKey()
        .ToView(
        "vw_ticket_validation_summary");

    modelBuilder.Entity<VwUserAccessMatrix>()
        .HasNoKey()
        .ToView(
        "vw_user_access_matrix");

    modelBuilder.Entity<VwUserApplicationMenu>()
        .HasNoKey()
        .ToView(
        "vw_user_application_menus");

    modelBuilder.Entity<VwAdminUserManagement>()
        .HasNoKey()
        .ToView(
        "vw_admin_user_management");



    base.OnModelCreating(
    modelBuilder);
}
    }
}
