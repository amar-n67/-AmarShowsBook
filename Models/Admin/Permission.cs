using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("permissions")]
    public class Permission
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("permission_code")]
        public string PermissionCode { get; set; }

        [Column("permission_name")]
        public string PermissionName { get; set; }

        [Column("module_id")]
        public long ModuleId { get; set; }

        [Column("action_type")]
        public string ActionType { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}