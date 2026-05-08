using AmarShowsBook.Services; // Added for activity logging
using Npgsql;                 // Added for PostgreSQL exception handling
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
        private readonly IActivityLogger _activityLogger; //Added for logging activities

        // ====================== commented out old constructor ======================
        // ✅ Inject BOTH logger + DB context
        // public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        // {
        //     _logger = logger;
        //     _context = context;
        // }
        // ====================== Updated constructor to include activity logger ======================
        public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        IActivityLogger activityLogger)
        {
            _logger = logger;
            _context = context;
            _activityLogger = activityLogger;
        }
        // ====================== End of updated constructor ======================

        //=================== commented out old Index action ======================
        // ================= HOME =================

        // public IActionResult Index(string type = "Movie")
        // {
        //     // 🔐 Protect page (only logged in users)
        //     if (HttpContext.Session.GetString("UserEmail") == null)
        //     {
        //         return RedirectToAction("Login", "Auth");
        //     }

        //     // 📦 Fetch schedules based on type
        //     var schedules = _context.ShowSchedules
        //         .Include(s => s.Movie)
        //         .Include(s => s.StandupShow)
        //         .Include(s => s.LiveStream)
        //         .Include(s => s.Location)
        //         .Where(s => s.Type == type)
        //         .OrderBy(s => s.StartTime)
        //         .ToList();

        //     var vm = new HomeViewModel
        //     {
        //         Schedules = schedules
        //     };

        //     return View(vm);
        // }
        // ====================== Updated Index action to log activity ======================
public async Task<IActionResult> Index(string type = "Movie")
{
    try
    {
        if (HttpContext.Session.GetString("UserEmail") == null)
        {
            await _activityLogger.LogAsync(
                action: "UNAUTHORIZED_ACCESS",
                module: "HOME",
                entityType: "PAGE",
                description: "Unauthorized access to home page",
                status: "FAILURE",
                isError: 4
            );

            return RedirectToAction("Login", "Auth");
        }

        var userEmail = HttpContext.Session.GetString("UserEmail");

        var user = _context.Users
            .FirstOrDefault(u => u.Email == userEmail);

        var schedules = _context.ShowSchedules
            .Include(s => s.Movie)
            .Include(s => s.StandupShow)
            .Include(s => s.LiveStream)
            .Include(s => s.Location)
            .Where(s => s.Type == type)
            .OrderBy(s => s.StartTime)
            .ToList();

        var vm = new HomeViewModel
        {
            Schedules = schedules
        };

        await _activityLogger.LogAsync(
            userId: user?.Id,
            action: "VIEW_HOME",
            module: "HOME",
            entityType: "SHOW_SCHEDULE",
            description: $"Viewed {type} schedules",
            status: "SUCCESS",
            isError: 0,
            metadata: new
            {
                Type = type,
                Count = schedules.Count
            }
        );

        return View(vm);
    }
    catch (PostgresException ex)
    {
        await _activityLogger.LogAsync(
            action: "VIEW_HOME",
            module: "HOME",
            entityType: "SHOW_SCHEDULE",
            description: "Database error while loading home page",
            status: "FAILURE",
            errorCode: ex.SqlState,
            errorMessage: ex.Message,
            errorSource: "PostgreSQL",
            stackTrace: ex.StackTrace,
            isError: 2
        );

        throw;
    }
    catch (Exception ex)
    {
        await _activityLogger.LogAsync(
            action: "VIEW_HOME",
            module: "HOME",
            entityType: "SHOW_SCHEDULE",
            description: "Unexpected error while loading home page",
            status: "FAILURE",
            errorCode: "APP500",
            errorMessage: ex.Message,
            errorSource: "Application",
            stackTrace: ex.StackTrace,
            isError: 1
        );

        throw;
    }
}
// ====================== End of updated Index action ======================
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
