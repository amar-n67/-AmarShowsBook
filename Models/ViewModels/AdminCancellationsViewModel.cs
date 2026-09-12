using AmarShowsBook.Models.Admin;

namespace AmarShowsBook.Models.ViewModels;

public class AdminCancellationsViewModel
{
    public List<AdminCancelableBookingViewModel> Bookings { get; set; } = new();

    public List<AdminCancelableShowViewModel> Shows { get; set; } = new();

    public List<AdminCancelableShowViewModel> RelaunchableShows { get; set; } = new();

    public List<AdminTicketCancellation> RecentCancellations { get; set; } = new();

    public List<AdminTicketCancellation> IndividualCancellationHistory { get; set; } = new();

    public List<AdminTicketCancellation> TheaterCancellationHistory { get; set; } = new();
}

public class AdminCancelableBookingViewModel
{
    public long BookingId { get; set; }

    public string BookingRef { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string ShowTitle { get; set; } = string.Empty;

    public string ShowType { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public string SeatNumbers { get; set; } = string.Empty;

    public int TotalTickets { get; set; }

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = string.Empty;
}

public class AdminCancelableShowViewModel
{
    public int ScheduleId { get; set; }

    public string ShowTitle { get; set; } = string.Empty;

    public string ShowType { get; set; } = string.Empty;

    public string VenueName { get; set; } = string.Empty;

    public string ScreenName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public int BookingCount { get; set; }

    public int TicketCount { get; set; }

    public decimal Amount { get; set; }

    public bool IsCancelled { get; set; }

    public long? CancellationId { get; set; }

    public string CancellationRef { get; set; } = string.Empty;

    public string CancellationReason { get; set; } = string.Empty;

    public DateTime? CancelledAt { get; set; }
}
