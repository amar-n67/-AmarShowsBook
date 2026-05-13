using System.ComponentModel.DataAnnotations;

namespace AmarShowsBook.Models
{
    public class User
    {
        public int Id { get; set; }

        // =====================================================
        // HUMAN COMMENT:
        // BASIC USER DETAILS
        // =====================================================

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? Genre { get; set; } = "Dramatic";

        public string? Language { get; set; } = "English";

        public string? ProfileImagePath { get; set; }

        // =====================================================
        // HUMAN COMMENT:
        // ADDRESS BREAKDOWN
        // =====================================================

        public string? Country { get; set; }

        public string? State { get; set; }

        public string? District { get; set; }

        public string? Pincode { get; set; }

        // =====================================================
        // HUMAN COMMENT:
        // AUDIT FIELDS
        // =====================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        // =====================================================
        // HUMAN COMMENT:
        // ADMIN USER CONTROL FLAGS
        // IMPORTANT:
        // KEEP ONLY THESE TWO PROPERTIES
        // DO NOT CREATE DUPLICATES
        // =====================================================

        public bool is_active { get; set; } = true;

        public bool is_deleted { get; set; } = false;

        // =====================================================
        // HUMAN COMMENT:
        // EMAIL VALIDATION
        // =====================================================

        [Required]
        [RegularExpression(
            @"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$",
            ErrorMessage = "Only Gmail or Outlook allowed")]
        public string Email { get; set; } = string.Empty;

        // =====================================================
        // HUMAN COMMENT:
        // PASSWORD VALIDATION
        // =====================================================

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8,
            ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "Password must contain uppercase, lowercase and special character")]
        public string Password { get; set; } = string.Empty;

        // =====================================================
        // HUMAN COMMENT:
        // MOBILE VALIDATION
        // =====================================================

        [Required(ErrorMessage = "Mobile is required")]
        [RegularExpression(
            @"^[0-9]{10}$",
            ErrorMessage = "Mobile must be exactly 10 digits")]
        public string Mobile { get; set; } = string.Empty;
    }
}