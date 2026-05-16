using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("application_menus")]
    public class ApplicationMenu
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("menu_code")]
        public string MenuCode { get; set; }

        [Column("menu_name")]
        public string MenuName { get; set; }

        [Column("parent_menu_id")]
        public long? ParentMenuId { get; set; }

        [Column("module_id")]
        public long? ModuleId { get; set; }

        [Column("route_path")]
        public string RoutePath { get; set; }

        [Column("icon_name")]
        public string IconName { get; set; }

        [Column("menu_level")]
        public int MenuLevel { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("is_visible")]
        public bool IsVisible { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}