using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin;

[Table("application_versions")]
public class ApplicationVersion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("version_number")]
    public string VersionNumber { get; set; } = string.Empty;

    [Column("release_title")]
    public string ReleaseTitle { get; set; } = string.Empty;

    [Column("release_notes")]
    public string? ReleaseNotes { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("is_current")]
    public bool IsCurrent { get; set; }
}
