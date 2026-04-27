using System.ComponentModel.DataAnnotations;

namespace AmarShowsBook.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
public string? Genre { get; set; } = "Dramatic";
public string? Language { get; set; } = "English";
        public string? ProfileImagePath { get; set; }
        // ADDRESS BREAKDOWN
public string? Country { get; set; }
public string? State { get; set; }
public string? District { get; set; }
public string? Pincode { get; set; }

// AUDIT
public string? CreatedBy { get; set; }
public string? UpdatedBy { get; set; }

[Required]
[RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$",
 ErrorMessage = "Only Gmail or Outlook allowed")]
public string Email { get; set; }


[Required(ErrorMessage = "Password is required")]
[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{6,}$",
 ErrorMessage = "Password must include upper, lower, number & special char")]
public string Password { get; set; }

        [Required(ErrorMessage = "Mobile is required")]
[RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile must be exactly 10 digits")]
public string Mobile { get; set; }
    }
}