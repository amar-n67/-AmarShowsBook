namespace AmarShowsBook.Models.ViewModels;

public class AdminUserDetailsViewModel
{
    public long UserId { get; set; }

    public string Name { get; set; } = "";

    public string Email { get; set; } = "";

    public string Mobile { get; set; } = "";

    public string Language { get; set; } = "";

    public string Genre { get; set; } = "";

    public string Country { get; set; } = "";

    public string State { get; set; } = "";

    public string District { get; set; } = "";

    public string Address { get; set; } = "";

    public string Pincode { get; set; } = "";

    public bool IsActive { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }
}