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

        // Normal checks allow the development bypass setting; strict checks are used for actions that should still prove role access.
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
            return IsSuperAdmin(userId);
        }

        public bool CanOpenAdminDashboard(int userId)
        {
            return HasAnyActiveRole(
                userId,
                "AMAR_SUPER_ADMIN",
                "AMAR_ADMIN",
                "AMAR_DEVELOPER",
                "DUM_ADMIN");
        }

        public bool CanAccessSuperAdminArea(int userId)
        {
            return HasAnyActiveRole(
                userId,
                "AMAR_SUPER_ADMIN",
                "AMAR_DEVELOPER");
        }

        public bool IsSuperAdmin(int userId)
        {
            return HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN");
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
            var bypass =
                _configuration.GetValue<bool>(
                    "DeveloperSettings:BypassRBAC");

            if (allowDeveloperBypass && bypass)
            {
                return true;
            }

            if (HasFullAccessRole(userId))
            {
                return true;
            }

            // Runtime permissions come from vw_user_access_matrix, which is rebuilt from the four fixed roles.
            return _context.VwUserAccessMatrices.Any(x =>
                x.UserId == userId &&
                x.ModuleCode == moduleCode &&
                x.ActionType == actionType &&
                x.IsActive
            );
        }


        // Admin navigation is also RBAC-driven, so hidden pages and blocked actions follow the same role source.
        public List<Models.VwUserApplicationMenu> GetMenus(
            int userId)
        {
            var bypass =
                _configuration.GetValue<bool>(
                    "DeveloperSettings:BypassRBAC");

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
                    (x.RoleCode == "AMAR_SUPER_ADMIN" ||
                     x.RoleCode == "AMAR_DEVELOPER"));
        }
    }
}
