using AmarShowsBook.Data;

namespace AmarShowsBook.Services
{
    public class RbacService
    {
        private readonly ApplicationDbContext _context;

        private readonly IConfiguration _configuration;

        public RbacService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ================= ROLE CHECK =================

        public bool HasPermission(
            int userId,
            string moduleCode,
            string actionType)
        {
            // Developer override mode
            var bypass =
                _configuration.GetValue<bool>(
                    "DeveloperSettings:BypassRBAC");

            // Allow everything in development
            if (bypass)
            {
                return true;
            }

            // Real RBAC validation
            return _context.VwUserAccessMatrices.Any(x =>
                x.UserId == userId &&
                x.ModuleCode == moduleCode &&
                x.ActionType == actionType &&
                x.IsActive
            );
        }

        // ================= MENU ACCESS =================

        public List<Models.VwUserApplicationMenu> GetMenus(
            int userId)
        {
            var bypass =
                _configuration.GetValue<bool>(
                    "DeveloperSettings:BypassRBAC");

            // Developer mode:
            // return all menus
            if (bypass)
            {
                return _context.VwUserApplicationMenus
                    .OrderBy(x => x.DisplayOrder)
                    .ToList();
            }

            // Production mode:
            // return user-specific menus
            return _context.VwUserApplicationMenus
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.DisplayOrder)
                .ToList();
        }
    }
}