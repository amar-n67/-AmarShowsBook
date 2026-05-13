using AmarShowsBook.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmarShowsBook.Services;

namespace AmarShowsBook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        // ======================
        // Constructor
        // Inject DB + Activity Logger
        // ======================
        public BookingController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // ======================
        // Seat Selection Page
        // ======================
        public IActionResult Seats(int id)
        {
            // Redirect guest users
            if (HttpContext.Session.GetString("UserEmail") == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var schedule = _context.ShowSchedules
                .Include(s => s.Movie)
                .Include(s => s.StandupShow)
                .Include(s => s.LiveStream)
                .Include(s => s.Location)
                .FirstOrDefault(s => s.Id == id);

            // Schedule not found
            if (schedule == null)
            {
                TempData["Error"] = "Selected show was not found.";

                return RedirectToAction("Index", "Home");
            }

            return View(schedule);
        }

        // ======================
        // My Bookings Page
        // ======================
public IActionResult MyBookings()
{
    var userEmail =
        HttpContext.Session.GetString("UserEmail");

    if (string.IsNullOrWhiteSpace(userEmail))
    {
        return RedirectToAction("Login", "Auth");
    }

    // =====================================================
    // HUMAN COMMENT:
    // Fetch bookings using proper PostgreSQL column mapping
    // =====================================================

    var bookings = _context.VwBookingCompleteDetails
        .Where(v => v.UserEmail == userEmail)
        .OrderByDescending(v => v.BookedAt)
        .ToList();

    return View(bookings);
}
    }
}