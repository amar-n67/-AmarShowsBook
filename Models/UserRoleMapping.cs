using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models;

[Table("user_role_mappings")]
public class UserRoleMapping
{
    
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("role_id")] 
    public long RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [Column("assigned_by")]
    public long? AssignedBy { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}