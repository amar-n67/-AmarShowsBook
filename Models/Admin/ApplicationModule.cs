using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("application_modules")]
public class ApplicationModule
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("module_code")]
    public string ModuleCode { get; set; } = string.Empty;

    [Column("module_name")]
    public string ModuleName { get; set; } = string.Empty;

    [Column("module_description")]
    public string? ModuleDescription { get; set; }

    [Column("route_path")]
    public string? RoutePath { get; set; }

    [Column("icon_name")]
    public string? IconName { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
