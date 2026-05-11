using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Represents dynamic user menu access SQL view
    [Keyless]
    public class VwUserApplicationMenu
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string RoleCode { get; set; }

        public int MenuId { get; set; }

        public string MenuCode { get; set; }

        public string MenuName { get; set; }

        public string RoutePath { get; set; }

        public string IconName { get; set; }

        public int MenuLevel { get; set; }

        public int DisplayOrder { get; set; }

        public bool CanView { get; set; }

        public bool CanCreate { get; set; }

        public bool CanUpdate { get; set; }

        public bool CanDelete { get; set; }
    }
}