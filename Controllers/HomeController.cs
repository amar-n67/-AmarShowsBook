using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.ViewModels;

namespace AmarShowsBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        // ✅ Inject BOTH logger + DB context
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ================= HOME =================

        public IActionResult Index(string type = "Movie")
        {
            // 🔐 Protect page (only logged in users)
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // 📦 Fetch schedules based on type
            var schedules = _context.ShowSchedules
                .Include(s => s.Movie)
                .Include(s => s.StandupShow)
                .Include(s => s.LiveStream)
                .Where(s => s.Type == type)
                .OrderBy(s => s.StartTime)
                .ToList();

            var vm = new HomeViewModel
            {
                Schedules = schedules
            };

            return View(vm);
        }

        // ================= PRIVACY =================

        public IActionResult Privacy()
        {
            return View();
        }

        // ================= ERROR =================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        [HttpGet]
public JsonResult GetStates(int countryId)
{
    var states = _context.States
        .Where(s => s.CountryId == countryId)
        .Select(s => new { id = s.Id, name = s.Name })
        .ToList();

    return Json(states);
}

[HttpGet]
public JsonResult GetDistricts(int stateId)
{
    var districts = _context.Districts
        .Where(d => d.StateId == stateId)
        .Select(d => new { id = d.Id, name = d.Name })
        .ToList();

    return Json(districts);
}
    }
}