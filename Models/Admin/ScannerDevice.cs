using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

[Table("scanner_devices")]
public class ScannerDevice
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("device_code")]
    public string DeviceCode { get; set; } = string.Empty;

    [Column("device_name")]
    public string? DeviceName { get; set; }

    [Column("venue_id")]
    public long? VenueId { get; set; }

    [Column("screen_id")]
    public long? ScreenId { get; set; }

    [Column("gate_name")]
    public string? GateName { get; set; }

    [Column("device_status")]
    public string? DeviceStatus { get; set; }

    [Column("last_active_at")]
    public DateTime? LastActiveAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
