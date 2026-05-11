using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Keyless]
    public class VwUserAccessMatrix
    {
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_name")]
        public string? UserName { get; set; }

        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("role_code")]
        public string? RoleCode { get; set; }

        [Column("role_name")]
        public string? RoleName { get; set; }

        [Column("module_code")]
        public string? ModuleCode { get; set; }

        [Column("module_name")]
        public string? ModuleName { get; set; }

        [Column("permission_code")]
        public string? PermissionCode { get; set; }

        [Column("permission_name")]
        public string? PermissionName { get; set; }

        [Column("action_type")]
        public string? ActionType { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}