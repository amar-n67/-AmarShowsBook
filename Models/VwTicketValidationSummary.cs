using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models
{
    [Keyless]
    public class VwTicketValidationSummary
    {
        [Column("validation_log_id")]
        public long ValidationLogId { get; set; }

        [Column("ticket_id")]
        public long TicketId { get; set; }

        [Column("ticket_number")]
        public string? TicketNumber { get; set; }

        [Column("booking_ref")]
        public string? BookingRef { get; set; }

        [Column("user_name")]
        public string? UserName { get; set; }

        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("validation_status")]
        public string? ValidationStatus { get; set; }

        [Column("validation_result")]
        public string? ValidationResult { get; set; }

        [Column("gate_name")]
        public string? GateName { get; set; }

        [Column("device_id")]
        public string? DeviceId { get; set; }

        [Column("scanner_user")]
        public string? ScannerUser { get; set; }

        [Column("scanned_at")]
        public DateTime? ValidatedAt { get; set; }

        [Column("validation_count")]
        public int ValidationCount { get; set; }

        [Column("last_scanned_at")]
        public DateTime? LastScannedAt { get; set; }

        [Column("is_security_issue")]
        public int IsSecurityIssue { get; set; }

        [NotMapped]
        public string? ShowTitle => null;

        [NotMapped]
        public string? ValidationMessage => ValidationResult;
    }
}
