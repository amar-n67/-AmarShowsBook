using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Represents user role permission matrix SQL view
    [Keyless]
    public class VwUserAccessMatrix
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string RoleCode { get; set; }

        public string RoleName { get; set; }

        public string ModuleCode { get; set; }

        public string ModuleName { get; set; }

        public string PermissionCode { get; set; }

        public string PermissionName { get; set; }

        public string ActionType { get; set; }

        public bool IsActive { get; set; }
    }
}