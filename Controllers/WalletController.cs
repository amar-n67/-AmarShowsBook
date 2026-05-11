using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers
{
    public class WalletController : Controller
    {
        // Temporary wallet page
        public IActionResult Index()
        {
            return View();
        }
    }
}