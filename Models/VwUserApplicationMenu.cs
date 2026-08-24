using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Keyless]
    public class VwUserApplicationMenu
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_name")]
        public string? UserName { get; set; }

        [Column("role_code")]
        public string? RoleCode { get; set; }

        [Column("menu_id")]
        public long MenuId { get; set; }

        [Column("menu_code")]
        public string? MenuCode { get; set; }

        [Column("menu_name")]
        public string? MenuName { get; set; }

        [Column("parent_menu_id")]
        public long? ParentMenuId { get; set; }

        [Column("parent_menu_name")]
        public string? ParentMenuName { get; set; }

        [Column("route_path")]
        public string? RoutePath { get; set; }

        [Column("icon_name")]
        public string? IconName { get; set; }

        [Column("menu_level")]
        public int MenuLevel { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("can_view")]
        public bool CanView { get; set; }

        [Column("can_create")]
        public bool CanCreate { get; set; }

        [Column("can_update")]
        public bool CanUpdate { get; set; }

        [Column("can_delete")]
        public bool CanDelete { get; set; }
    }
}
