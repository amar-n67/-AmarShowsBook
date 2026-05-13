namespace AmarShowsBook.Models;

public class UserRoleMapping
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long RoleId { get; set; }

    public DateTime AssignedAt { get; set; }

    public long? AssignedBy { get; set; }

    public bool IsActive { get; set; }
}