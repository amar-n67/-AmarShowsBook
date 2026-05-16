using AmarShowsBook.Services;

namespace AmarShowsBook.Helpers
{
    public static class RbacAuthorizationHelper
    {
        // Central permission validation helper
        public static bool CanAccess(
            HttpContext context,
            RbacService rbacService,
            string moduleCode,
            string actionType)
        {
            var userIdText =
                context.Session.GetString("UserId");

            if (!int.TryParse(userIdText, out int userId))
            {
                return false;
            }

            return rbacService.HasPermission(
                userId,
                moduleCode,
                actionType
            );
        }
    }
}