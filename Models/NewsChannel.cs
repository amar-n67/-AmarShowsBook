using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("news_channels")]
public class NewsChannel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("channel_code")]
    public string ChannelCode { get; set; } = string.Empty;

    [Column("channel_name")]
    public string ChannelName { get; set; } = string.Empty;

    [Column("language")]
    public string Language { get; set; } = string.Empty;

    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("region")]
    public string Region { get; set; } = string.Empty;

    [Column("country")]
    public string Country { get; set; } = string.Empty;

    [Column("state")]
    public string State { get; set; } = string.Empty;

    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("logo_url")]
    public string? LogoUrl { get; set; }

    [Column("website_url")]
    public string? WebsiteUrl { get; set; }

    [Column("live_url")]
    public string? LiveUrl { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
