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

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public WalletController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // =====================================================
        // WALLET DASHBOARD
        // =====================================================

        public async Task<IActionResult> Index()
        {
            // Get logged in user email
            var userEmail =
                HttpContext.Session.GetString("UserEmail");

            // Redirect guest users
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            // Get wallet summary from view
            var wallet = _context
                .VwWalletSummaries
                .FirstOrDefault(x =>
                    x.UserEmail == userEmail);

            // Wallet not found
            if (wallet == null)
            {
                TempData["Error"] =
                    "Wallet information not found.";

                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }

            // Store activity log
            await _activityLogger.LogAsync(
                userId: (int)wallet.UserId,
                action: "VIEW_WALLET",
                module: "WALLET",
                entityType: "WALLET",
                entityId: (int)wallet.WalletId,
                description:
                    "User viewed wallet dashboard",
                status: "SUCCESS",
                isError: 0
            );

            // Return wallet page
            return View(wallet);
        }
    }
}