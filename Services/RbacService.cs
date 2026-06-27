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
            return HasPermissionCore(
                userId,
                moduleCode,
                actionType,
                allowDeveloperBypass: true);
        }

        public bool HasPermissionStrict(
            int userId,
            string moduleCode,
            string actionType)
        {
            return HasPermissionCore(
                userId,
                moduleCode,
                actionType,
                allowDeveloperBypass: false);
        }

        public bool CanUsePrintTools(int userId)
        {
            return HasAnyActiveRole(
                    userId,
                    "AMAR_SUPER_ADMIN",
                    "AMAR_ADMIN",
                    "ADMIN",
                    "AMAR_DEVELOPER",
                    "DEVELOPER")
                || HasPermissionStrict(userId, "ADMIN", "VIEW")
                || HasPermissionStrict(userId, "DEVELOPER", "EDIT");
        }

        public bool HasAnyActiveRole(
            int userId,
            params string[] roleCodes)
        {
            var lookup = roleCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!lookup.Any())
            {
                return false;
            }

            return _context.UserRoleMappings
                .Join(
                    _context.Roles,
                    mapping => mapping.RoleId,
                    role => role.Id,
                    (mapping, role) => new
                    {
                        mapping.UserId,
                        mapping.IsActive,
                        role.RoleCode,
                        RoleIsActive = role.IsActive
                    })
                .Any(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    x.RoleIsActive &&
                    lookup.Contains(x.RoleCode));
        }

        private bool HasPermissionCore(
            int userId,
            string moduleCode,
            string actionType,
            bool allowDeveloperBypass)
        {
            // Developer override mode
            var bypass =
                _configuration.GetValue<bool>(
                    "DeveloperSettings:BypassRBAC");

            // Allow everything in development
            if (allowDeveloperBypass && bypass)
            {
                return true;
            }

            if (HasFullAccessRole(userId))
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

            if (HasFullAccessRole(userId))
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

        private bool HasFullAccessRole(int userId)
        {
            return _context.UserRoleMappings
                .Join(
                    _context.Roles,
                    mapping => mapping.RoleId,
                    role => role.Id,
                    (mapping, role) => new
                    {
                        mapping.UserId,
                        mapping.IsActive,
                        role.RoleCode,
                        RoleIsActive = role.IsActive
                    })
                .Any(x =>
                    x.UserId == userId &&
                    x.IsActive &&
                    x.RoleIsActive &&
                    x.RoleCode == "AMAR_SUPER_ADMIN");
        }
    }
}
