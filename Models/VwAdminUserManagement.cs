using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Represents admin user management analytics view
    [Keyless]
    public class VwAdminUserManagement
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string UserMobile { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public int TotalBookings { get; set; }

        public decimal TotalSpent { get; set; }
    }
}