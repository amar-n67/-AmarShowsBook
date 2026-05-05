using AmarShowsBook.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

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
    }
}
