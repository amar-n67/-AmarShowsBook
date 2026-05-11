using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Services;
using System.Linq;

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

            var wallet = _context
                .VwWalletSummary
                .FirstOrDefault(x => x.UserEmail == userEmail);

            // Wallet not found
            if (wallet == null)
            {
                TempData["Error"] =
                    "Wallet information could not be found.";

                return RedirectToAction("Index", "Home");
            }

            // Store wallet activity log
            await _activityLogger.LogAsync(
                userId: wallet.UserId,
                action: "VIEW_WALLET",
                module: "WALLET",
                entityType: "WALLET",
                entityId: (int)wallet.WalletId,
                description: "User viewed wallet dashboard",
                status: "SUCCESS",
                isError: 0
            );

            return View(wallet);
        }
    }
}