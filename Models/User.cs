using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        // public string Name { get; set; } // commeent to handle nullability in the database
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        // Admin user status management
// Human Comment:
// PostgreSQL uses snake_case column names.
// Explicit mapping avoids EF Core naming mismatch.

[Column("is_active")]
public bool IsActive { get; set; } = true;

[Column("is_deleted")]
public bool IsDeleted { get; set; } = false;
[Required(ErrorMessage = "Mobile is required")]
[RegularExpression(@"^[0-9]{10}$",
 ErrorMessage = "Mobile must be exactly 10 digits")]

// =====================================================
// ADMIN CONTROL FLAGS
// Human Comment:
// These flags are used by admin dashboard
// to enable/disable/delete users safely.
// =====================================================

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
//public string Email { get; set; } //comment to handle nullability in the database
public string Email { get; set; } = string.Empty;


[Required(ErrorMessage = "Password is required")]
[MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$",
 ErrorMessage = "Password must be at least 8 characters and include uppercase, lowercase, and special character")]
//public string Password { get; set; } //comment to handle nullability in the database
public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile is required")]
[RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile must be exactly 10 digits")]
//public string Mobile { get; set; }//comment to handle nullability in the database
public string Mobile { get; set; } = string.Empty;
    }
}
