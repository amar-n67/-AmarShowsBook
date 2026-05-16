namespace AmarShowsBook.Models.ViewModels;

public class AdminUserRoleViewModel
{
    public long UserId { get; set; }

    public string? UserName { get; set; }

    public string? UserEmail { get; set; }

    public bool IsActive { get; set; }

    public List<string> Roles { get; set; } = new();

    public List<long> RoleIds { get; set; } = new();
}