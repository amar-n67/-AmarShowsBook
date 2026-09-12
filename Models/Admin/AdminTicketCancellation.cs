using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

[Table("admin_ticket_cancellations")]
public class AdminTicketCancellation
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("cancellation_ref")]
    public string CancellationRef { get; set; } = string.Empty;

    [Column("scope")]
    public string Scope { get; set; } = string.Empty;

    [Column("booking_id")]
    public long? BookingId { get; set; }

    [Column("schedule_id")]
    public int? ScheduleId { get; set; }

    [Column("show_title")]
    public string? ShowTitle { get; set; }

    [Column("show_type")]
    public string? ShowType { get; set; }

    [Column("venue_name")]
    public string? VenueName { get; set; }

    [Column("screen_name")]
    public string? ScreenName { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("affected_bookings")]
    public int AffectedBookings { get; set; }

    [Column("affected_tickets")]
    public int AffectedTickets { get; set; }

    [Column("requested_by_user_id")]
    public long? RequestedByUserId { get; set; }

    [Column("requested_by_name")]
    public string? RequestedByName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("is_revoked")]
    public bool IsRevoked { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoked_by_user_id")]
    public long? RevokedByUserId { get; set; }

    [Column("revoked_by_name")]
    public string? RevokedByName { get; set; }

    [Column("relaunch_reason")]
    public string? RelaunchReason { get; set; }

    [Column("relaunch_same_time")]
    public bool RelaunchSameTime { get; set; } = true;

    [Column("relaunch_original_start_time")]
    public DateTime? RelaunchOriginalStartTime { get; set; }

    [Column("relaunch_original_end_time")]
    public DateTime? RelaunchOriginalEndTime { get; set; }

    [Column("relaunch_new_start_time")]
    public DateTime? RelaunchNewStartTime { get; set; }

    [Column("relaunch_new_end_time")]
    public DateTime? RelaunchNewEndTime { get; set; }
}
