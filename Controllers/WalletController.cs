using AmarShowsBook.Data;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers
{
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IActivityLogger _activityLogger;

        // Inject database context and activity logger
        public WalletController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // ================= WALLET DASHBOARD =================

        public async Task<IActionResult> Index()
        {
            // Get logged-in user email from session
            var userEmail =
                HttpContext.Session.GetString("UserEmail");

            // Prevent guest access
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Load wallet summary from database view
            var wallet = _context.VwWalletSummaries
                .FirstOrDefault(x => x.UserEmail == userEmail);

            // Handle wallet not found
            if (wallet == null)
            {
                TempData["Error"] =
                    "Wallet details are not available.";

                return RedirectToAction("Index", "Home");
            }

            // Log successful wallet access
            await _activityLogger.LogAsync(
                userId: wallet.UserId,
                action: "VIEW_WALLET",
                module: "WALLET",
                entityType: "WALLET",
                entityId: wallet.WalletId,
                description: "User viewed wallet dashboard",
                status: "SUCCESS",
                isError: 0
            );

            return View(wallet);
        }
    }
}