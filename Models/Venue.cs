using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("venues")]
public class Venue
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("venue_code")]
    public string VenueCode { get; set; } = string.Empty;

    [Column("venue_name")]
    public string VenueName { get; set; } = string.Empty;

    [Column("venue_type")]
    public string? VenueType { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("state")]
    public string? State { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("postal_code")]
    public string? PostalCode { get; set; }

    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [Column("contact_mobile")]
    public string? ContactMobile { get; set; }

    [Column("total_screens")]
    public int TotalScreens { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
