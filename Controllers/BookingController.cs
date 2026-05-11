using AmarShowsBook.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmarShowsBook.Services;

namespace AmarShowsBook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IActionResult Seats(int id)
        {
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

            if (schedule == null)
            {
                TempData["Error"] = "Selected show was not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(schedule);
        }
        private readonly IActivityLogger _activityLogger;

        public BookingController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
            {
                _context = context;
                _activityLogger = activityLogger;
            }
    }
    public IActionResult MyBookings()
{
    var userEmail = HttpContext.Session.GetString("UserEmail");

    // Redirect guest users to login
    if (string.IsNullOrWhiteSpace(userEmail))
    {
        return RedirectToAction("Login", "Auth");
    }

    var bookings = _context
        .VwBookingCompleteDetails
        .Where(x => x.UserEmail == userEmail)
        .OrderByDescending(x => x.BookedAt)
        .ToList();

    return View(bookings);
}
}
