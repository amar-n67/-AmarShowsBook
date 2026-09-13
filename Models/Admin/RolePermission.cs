using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("role_permissions")]
public class RolePermission
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("role_id")]
    public long RoleId { get; set; }

    [Column("permission_id")]
    public long PermissionId { get; set; }

    [Column("granted_by")]
    public long? GrantedBy { get; set; }

    [Column("granted_at")]
    public DateTime GrantedAt { get; set; }
}
