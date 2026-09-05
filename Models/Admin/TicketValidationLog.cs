using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

[Table("ticket_validation_logs")]
public class TicketValidationLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("ticket_id")]
    public long TicketId { get; set; }

    [Column("booking_id")]
    public long? BookingId { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("scanned_qr_token")]
    public Guid? ScannedQrToken { get; set; }

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

    [Column("scanner_ip")]
    public string? ScannerIp { get; set; }

    [Column("latitude")]
    public decimal? Latitude { get; set; }

    [Column("longitude")]
    public decimal? Longitude { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("metadata")]
    public string? Metadata { get; set; }

    [Column("scanned_at")]
    public DateTime? ScannedAt { get; set; }
}
