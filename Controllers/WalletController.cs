using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Services;

namespace AmarShowsBook.Controllers
{
    // Customer wallet pages read the wallet summary view; balance changes are written by booking and admin flows.
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;


        public WalletController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }


        public IActionResult Index()
        {
            return RedirectToAction(nameof(MyWallet));
        }

        public async Task<IActionResult> MyWallet()
        {
            var userEmail =
                HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Auth"
                );
            }

            var wallet = _context
                .VwWalletSummaries
                .FirstOrDefault(x =>
                    x.UserEmail == userEmail);

            if (wallet == null)
            {
                TempData["Error"] =
                    "Wallet information not found.";

                return RedirectToAction(
                    "ShowTime",
                    "Home"
                );
            }

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

            return View("Index", wallet);
        }
    }
}
