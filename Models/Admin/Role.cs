using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("role_code")]
        public string RoleCode { get; set; }

        [Column("role_name")]
        public string RoleName { get; set; }

        [Column("role_description")]
        public string? RoleDescription { get; set; }

        [Column("is_system_role")]
        public bool IsSystemRole { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("updated_by")]
        public string? UpdatedBy { get; set; }
    }
}